using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using NewConstructions = System.Collections.Immutable.ImmutableDictionary<ushort, System.Collections.Immutable.ImmutableDictionary<Simulturn.Core.Model.Hexagon, Simulturn.Core.Model.Compound>.Builder>.Builder;
using NewTrainings = System.Collections.Immutable.ImmutableDictionary<ushort, System.Collections.Immutable.ImmutableDictionary<Simulturn.Core.Model.Hexagon, Simulturn.Core.Model.Army>.Builder>.Builder;

namespace Simulturn.Core.Engine;

/// <summary>
/// Resolves one turn: a game state plus the commands of all players gives the next game state
/// and the report of what happened. An instance holds the working state of exactly one resolution.
/// </summary>
public sealed class TurnResolution
{
    private readonly GameState _state;
    private readonly GameSettings _settings;
    private readonly Dictionary<string, PlayerStateBuilder> _players;
    private readonly ImmutableDictionary<Hexagon, int>.Builder _remainingMatter;
    private readonly HashSet<Hexagon> _potentialFightHexagons = [];
    private readonly ImmutableDictionary<Hexagon, FightReport>.Builder _fights = ImmutableDictionary.CreateBuilder<Hexagon, FightReport>();
    private readonly ImmutableArray<DestructionReport>.Builder _destructions = ImmutableArray.CreateBuilder<DestructionReport>();

    private TurnResolution(GameState state)
    {
        _state = state;
        _settings = state.GameSettings;
        _players = state.PlayerStates.ToDictionary(x => x.Key, x => PlayerStateBuilder.FromPlayerState(x.Value));
        _remainingMatter = state.RemainingMatter.ToBuilder();
    }

    /// <summary>
    /// Resolves one turn. The commands of every player must have passed <see cref="CommandValidator.Validate"/>:
    /// the resolution applies them without checking and an invalid command, for example a research
    /// beyond the last level of <see cref="GameSettings.Upgrades"/>, corrupts the state or throws.
    /// </summary>
    public static TurnResult Resolve<TCommands>(GameState state, IReadOnlyDictionary<string, TCommands> commands)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        TurnResolution resolution = new(state);
        foreach (string playerId in state.PlayerIds)
        {
            resolution.ResolveOrders(resolution._players[playerId], commands.GetOrDefault(playerId));
        }
        resolution.ResolveFights();
        resolution.DestroyBuildings();
        resolution.CancelResearchesWithoutDot();
        return new TurnResult(resolution.BuildNextState(), resolution.BuildReport());
    }

    private void ResolveOrders(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        NewConstructions newConstructions = GetNewConstructions(commands);
        NewTrainings newTrainings = GetNewTrainings(commands);
        CollectIncome(player, newConstructions);
        ResolveTrainings(player, newTrainings);
        ResolveConstructions(player, newConstructions);
        ResolveResearches(player, commands);
        MoveArmies(player, commands);
    }

    /// <summary>
    /// Dots that start a construction this turn do not harvest.
    /// </summary>
    private void CollectIncome(PlayerStateBuilder player, NewConstructions newConstructions)
    {
        foreach ((Hexagon hexagon, Army army) in player.Armies)
        {
            if (!player.Compounds.TryGetValue(hexagon, out var compound) || compound.Plane == 0)
            {
                continue;
            }
            Compound constructions = newConstructions
                .Select(x => x.Value.GetValueOrDefault(hexagon))
                .Sum();

            var idleArmy = army with
            {
                Dot = (short)Math.Max(0, army.Dot - constructions.Sum())
            };
            idleArmy = Army.Min(idleArmy, _settings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter);
            int hexagonIncome = _settings.Income * idleArmy;
            hexagonIncome = Math.Min(hexagonIncome, _remainingMatter[hexagon]);
            if (hexagonIncome <= 0)
            {
                continue;
            }
            _remainingMatter[hexagon] -= hexagonIncome;
            player.Matter += hexagonIncome;
            player.Report.Incomes.Add(hexagon, new IncomeReport(idleArmy, hexagonIncome));
        }
    }

    private void ResolveTrainings(PlayerStateBuilder player, NewTrainings newTrainings)
    {
        player.UsedSpace += newTrainings.Values.SelectMany(x => x.Values).Sum(army => army * _settings.RequiredSpace);
        player.Trainings.Merge(newTrainings);
        if (player.Trainings.TryGetValue(_state.Turn, out var trainings))
        {
            player.Armies.Merge(trainings);
            player.Report.CompletedTrainings = trainings.ToImmutable();
            _potentialFightHexagons.UnionWith(trainings.Keys); // The army might be trained on a hexagon with an enemy army
            player.Trainings.Remove(_state.Turn);
        }
        player.Matter -= newTrainings.Sum(x => x.Value.Values.Sum(training => training * _settings.ArmyCost));
    }

    private void ResolveConstructions(PlayerStateBuilder player, NewConstructions newConstructions)
    {
        player.Constructions.Merge(newConstructions);
        if (player.Constructions.TryGetValue(_state.Turn, out var construction))
        {
            player.Compounds.Merge(construction);
            player.Report.CompletedConstructions = construction.ToImmutable();
            player.AvailableSpace += construction.Values.Sum(compound => compound * _settings.ProvidedSpace);
            player.Constructions.Remove(_state.Turn);
        }
        player.Matter -= newConstructions.Sum(x => x.Value.Values.Sum(compound => compound * _settings.CompoundCost));
    }

    /// <summary>
    /// Pays and queues the new researches, then completes the ones due this turn.
    /// Nothing stacks (one pending research per upgrade), so the next level is the one after the completed ones.
    /// </summary>
    private void ResolveResearches(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        foreach ((Hexagon hexagon, Command command) in commands)
        {
            if (command.Research is not Upgrade upgrade)
            {
                continue;
            }
            byte completedLevels = player.UpgradeLevels.GetValueOrDefault(upgrade);
            var level = _settings.Upgrades[upgrade][completedLevels]; // a level of 1 means the first upgrade, which is at index 0
            // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
            player.Researches.Add(hexagon, new Research(upgrade, (ushort)(_state.Turn + level.Duration - 1)));
            player.Matter -= level.Cost;
        }
        foreach ((Hexagon hexagon, Research research) in player.Researches.Where(x => x.Value.CompletionTurn == _state.Turn).ToList())
        {
            byte level = (byte)(player.UpgradeLevels.GetValueOrDefault(research.Upgrade) + 1);
            player.UpgradeLevels[research.Upgrade] = level;
            player.Researches.Remove(hexagon);
            player.Report.CompletedResearches.Add(hexagon, new ResearchReport(research.Upgrade, level));
        }
    }

    private void MoveArmies(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        foreach ((Hexagon hexagon, Command command) in commands)
        {
            foreach (MovementCommand movement in command.MovementCommands)
            {
                if (movement.Army.IsEmpty)
                {
                    continue;
                }
                player.Armies.Merge(hexagon, -movement.Army);
                player.Armies.Merge(movement.Destination, movement.Army);
                player.Report.Movements.Add(new MovementReport(hexagon, movement.Destination, movement.Army));
                _potentialFightHexagons.Add(movement.Destination);
            }
        }
    }

    private void ResolveFights()
    {
        foreach (Hexagon hexagon in _potentialFightHexagons)
        {
            List<(string PlayerId, Army Army)> armies = GetArmiesOn(hexagon);
            if (armies.Count < 2)
            {
                continue;
            }
            Dictionary<string, Army> losses = CalculateLosses(armies);
            _fights.Add(hexagon, new FightReport()
            {
                Armies = armies.ToImmutableDictionary(x => x.PlayerId, x => x.Army),
                Losses = armies.ToImmutableDictionary(x => x.PlayerId, x => losses.GetValueOrDefault(x.PlayerId))
            });
            foreach ((string playerId, Army loss) in losses)
            {
                ApplyLosses(_players[playerId], hexagon, loss);
            }
        }
    }

    private List<(string PlayerId, Army Army)> GetArmiesOn(Hexagon hexagon)
    {
        List<(string PlayerId, Army Army)> armies = [];
        foreach (string playerId in _state.PlayerIds)
        {
            if (_players[playerId].Armies.TryGetValue(hexagon, out var army))
            {
                armies.Add((playerId, army));
            }
        }
        return armies;
    }

    /// <summary>
    /// Every army fights every other army on the hexagon with its full strength; the losses of a player add up.
    /// </summary>
    private Dictionary<string, Army> CalculateLosses(List<(string PlayerId, Army Army)> armies)
    {
        Dictionary<string, Army> losses = [];
        for (var i = 0; i < armies.Count - 1; i++)
        {
            for (int j = i + 1; j < armies.Count; j++)
            {
                (Army lossesPlayerI, Army lossesPlayerJ) = Combat.Fight(armies[i].Army,
                    GetFightExponent(armies[i].PlayerId),
                    armies[j].Army,
                    GetFightExponent(armies[j].PlayerId));
                losses.Merge(armies[i].PlayerId, lossesPlayerI);
                losses.Merge(armies[j].PlayerId, lossesPlayerJ);
            }
        }
        return losses;
    }

    /// <summary>
    /// The upgrade bonus comes from the state at the start of the turn: a research completed
    /// this turn counts from the next turn on.
    /// </summary>
    private Army GetFightExponent(string playerId)
    {
        return _settings.FightExponent + _state.PlayerStates[playerId].ExponentBonusFromUpgrades(_settings);
    }

    private void ApplyLosses(PlayerStateBuilder player, Hexagon hexagon, Army loss)
    {
        player.Armies.Merge(hexagon, -loss);
        player.UsedSpace -= loss * _settings.RequiredSpace;
        if (loss.Dot > 0)
        {
            CancelConstructions(player, hexagon, loss.Dot);
        }
    }

    /// <summary>
    /// Every lost dot stops one construction on the hexagon, the latest completion turn first.
    /// </summary>
    private static void CancelConstructions(PlayerStateBuilder player, Hexagon hexagon, short dotLosses)
    {
        var canceledConstructionCandidates = player.Constructions.OrderByDescending(x => x.Key);
        foreach ((ushort turn, var constructionForTurn) in canceledConstructionCandidates)
        {
            if (!constructionForTurn.TryGetValue(hexagon, out var construction) || construction.IsEmpty)
            {
                continue;
            }
            Compound newConstruction = construction;
            foreach (var building in EnumValues.Buildings)
            {
                if (newConstruction[building] <= 0)
                {
                    continue;
                }
                Compound cancel = Compound.FromBuilding(building, Math.Min(newConstruction[building], dotLosses));
                dotLosses -= cancel[building];
                newConstruction -= cancel;
                if (!cancel.IsEmpty)
                {
                    player.Report.CanceledConstructions.Merge(hexagon, cancel);
                }
                if (dotLosses <= 0)
                {
                    break;
                }
            }
            if (newConstruction.IsEmpty)
            {
                player.Constructions[turn].Remove(hexagon);
                if (player.Constructions[turn].Count == 0)
                {
                    player.Constructions.Remove(turn);
                }
            }
            else
            {
                player.Constructions[turn][hexagon] = newConstruction;
            }
        }
    }

    private void DestroyBuildings()
    {
        foreach (Hexagon hexagon in _state.Hexagons)
        {
            foreach (string attackerId in _state.PlayerIds)
            {
                foreach (string defenderId in _state.PlayerIds)
                {
                    if (attackerId == defenderId)
                    {
                        continue;
                    }
                    PlayerStateBuilder defender = _players[defenderId];
                    if (!_players[attackerId].Armies.TryGetValue(hexagon, out var army) || army.IsEmpty ||
                        !defender.Compounds.TryGetValue(hexagon, out var compound) || compound.IsEmpty)
                    {
                        continue;
                    }
                    Compound destroyedCompound = GetDestroyedBuildings(army, compound);
                    if (destroyedCompound.IsEmpty)
                    {
                        continue;
                    }
                    defender.Compounds.Merge(hexagon, -destroyedCompound);
                    defender.AvailableSpace -= destroyedCompound * _settings.ProvidedSpace;
                    _destructions.Add(new DestructionReport(hexagon, attackerId, defenderId, destroyedCompound));
                    CancelTrainings(defender, hexagon, destroyedCompound);
                }
            }
        }
    }

    /// <summary>
    /// The damage of the army is spent on the buildings with the lowest armor first.
    /// </summary>
    private Compound GetDestroyedBuildings(Army army, Compound compound)
    {
        double damage = _settings.StructureDamage * army;
        var buildings = EnumValues.Buildings.OrderBy(x => _settings.Armor[x]);
        var destroyedCompound = Compound.Empty;
        foreach (var building in buildings)
        {
            var destroyedBuildings = (short)Math.Min(compound[building], Math.Floor(damage / (double)_settings.Armor[building]));
            if (destroyedBuildings <= 0)
            {
                continue;
            }
            damage -= destroyedBuildings * _settings.Armor[building];
            destroyedCompound = destroyedCompound + Compound.FromBuilding(building, destroyedBuildings);
        }
        return destroyedCompound;
    }

    /// <summary>
    /// Every destroyed building cancels one training that needs it and refunds the space.
    /// </summary>
    private void CancelTrainings(PlayerStateBuilder player, Hexagon hexagon, Compound destroyedCompound)
    {
        foreach (var building in EnumValues.Buildings)
        {
            short destroyedCount = destroyedCompound[building];
            if (destroyedCount <= 0)
            {
                continue;
            }
            foreach ((ushort turn, var candidate) in player.Trainings)
            {
                if (destroyedCount <= 0 || !candidate.TryGetValue(hexagon, out Army training) || training.IsEmpty)
                {
                    continue;
                }
                foreach (Unit unit in EnumValues.Units)
                {
                    if (destroyedCount <= 0 ||
                        training[unit] <= 0 ||
                        GameSettings.TrainingBuilding(unit) != building)
                    {
                        continue;
                    }
                    short canceledCount = (short)Math.Min(training[unit], destroyedCount);
                    player.UsedSpace -= canceledCount * _settings.RequiredSpace[unit];
                    destroyedCount -= canceledCount;
                    Army canceled = Army.FromUnit(unit, canceledCount);
                    player.Trainings[turn].Merge(hexagon, -canceled);
                    player.Report.CanceledTrainings.Merge(hexagon, canceled);
                }
            }
        }
    }

    /// <summary>
    /// Researches without a dot are canceled, the matter is lost. Checked once all movements and
    /// fights are resolved, so a dot arriving in the same turn keeps the research alive.
    /// </summary>
    private void CancelResearchesWithoutDot()
    {
        foreach (PlayerStateBuilder player in _players.Values)
        {
            foreach ((Hexagon hexagon, Research research) in player.Researches.ToList())
            {
                if (player.Armies.GetValueOrDefault(hexagon).Dot <= 0)
                {
                    player.Researches.Remove(hexagon);
                    player.Report.CanceledResearches.Add(hexagon, research.Upgrade);
                }
            }
        }
    }

    private GameState BuildNextState()
    {
        if (_state.Turn == ushort.MaxValue)
        {
            throw new InvalidOperationException("Max turn limit reached.");
        }
        ushort nextTurn = (ushort)(1 + _state.Turn);
        var remainingMatter = _remainingMatter.ToImmutableDictionary();
        var playerStates = _players.ToImmutableDictionary(x => x.Key, x => x.Value.ToPlayerState());
        return GameState.FromParts(_settings, nextTurn, playerStates, remainingMatter);
    }

    private TurnReport BuildReport()
    {
        return new TurnReport()
        {
            Activities = _players.ToImmutableDictionary(
                x => x.Key,
                x => x.Value.Report.ToReport(_state.PlayerStates[x.Key].Matter, x.Value.Matter)),
            Fights = _fights.ToImmutable(),
            Destructions = _destructions.ToImmutable()
        };
    }

    private NewTrainings GetNewTrainings(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        NewTrainings newTrainings = ImmutableDictionary.CreateBuilder<ushort, ImmutableDictionary<Hexagon, Army>.Builder>();
        foreach ((var hexagon, var command) in commands.Where(x => !x.Value.Training.IsEmpty))
        {
            Army training = command.Training;
            foreach (var unit in EnumValues.Units)
            {
                if (training[unit] <= 0)
                {
                    continue;
                }
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(_state.Turn + _settings.TrainingDuration[unit] - 1);
                if (!newTrainings.TryGetValue(completionTurn, out var turnDict))
                {
                    turnDict = ImmutableDictionary.CreateBuilder<Hexagon, Army>();
                    newTrainings.Add(completionTurn, turnDict);
                }

                var trainedUnit = Army.FromUnit(unit, training[unit]);
                turnDict.Merge(hexagon, trainedUnit);
            }
        }
        return newTrainings;
    }

    private NewConstructions GetNewConstructions(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        NewConstructions newConstructions = ImmutableDictionary.CreateBuilder<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>();
        foreach ((var hexagon, var command) in commands.Where(x => !x.Value.Construction.IsEmpty))
        {
            Compound construction = command.Construction;
            foreach (var building in EnumValues.Buildings)
            {
                if (construction[building] <= 0)
                {
                    continue;
                }
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(_state.Turn + _settings.ConstructionDuration[building] - 1);
                if (!newConstructions.TryGetValue(completionTurn, out var turnDict))
                {
                    turnDict = ImmutableDictionary.CreateBuilder<Hexagon, Compound>();
                    newConstructions.Add(completionTurn, turnDict);
                }
                var constructedBuilding = Compound.FromBuilding(building, construction[building]);
                turnDict.Merge(hexagon, constructedBuilding);
            }
        }
        return newConstructions;
    }
}
