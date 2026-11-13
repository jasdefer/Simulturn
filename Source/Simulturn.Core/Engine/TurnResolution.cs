using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

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
        CollectIncome(player, commands);
        ResolveTrainings(player, commands);
        ResolveConstructions(player, commands);
        ResolveResearches(player, commands);
        MoveArmies(player, commands);
    }

    /// <summary>
    /// Dots that start a construction this turn do not harvest.
    /// </summary>
    private void CollectIncome(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        foreach ((Hexagon hexagon, Army army) in player.Armies)
        {
            if (!player.Compounds.TryGetValue(hexagon, out var compound) || compound.Plane == 0)
            {
                continue;
            }
            int startedConstructions = commands.TryGetValue(hexagon, out Command? command)
                ? EnumValues.Buildings.Sum(building => Math.Max(0, (int)command.Construction[building]))
                : 0;

            var idleArmy = army with
            {
                Dot = (short)Math.Max(0, army.Dot - startedConstructions)
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

    /// <summary>
    /// Pays and queues the new trainings, then completes the ones due this turn.
    /// </summary>
    private void ResolveTrainings(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        foreach ((Hexagon hexagon, Command command) in commands)
        {
            foreach (Unit unit in EnumValues.Units)
            {
                if (command.Training[unit] <= 0)
                {
                    continue;
                }
                Army training = Army.FromUnit(unit, command.Training[unit]);
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(_state.Turn + _settings.TrainingDuration[unit] - 1);
                player.Trainings.Merge(new TurnHexagon(completionTurn, hexagon), training);
                player.UsedSpace += training * _settings.RequiredSpace;
                player.Matter -= training * _settings.ArmyCost;
            }
        }
        foreach ((TurnHexagon key, Army training) in player.Trainings.Where(x => x.Key.Turn == _state.Turn).ToList())
        {
            player.Trainings.Remove(key);
            player.Armies.Merge(key.Hexagon, training);
            player.Report.CompletedTrainings.Add(key.Hexagon, training);
            _potentialFightHexagons.Add(key.Hexagon); // The army might be trained on a hexagon with an enemy army
        }
    }

    /// <summary>
    /// Pays and queues the new constructions, then completes the ones due this turn.
    /// </summary>
    private void ResolveConstructions(PlayerStateBuilder player, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        foreach ((Hexagon hexagon, Command command) in commands)
        {
            foreach (Building building in EnumValues.Buildings)
            {
                if (command.Construction[building] <= 0)
                {
                    continue;
                }
                Compound construction = Compound.FromBuilding(building, command.Construction[building]);
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(_state.Turn + _settings.ConstructionDuration[building] - 1);
                player.Constructions.Merge(new TurnHexagon(completionTurn, hexagon), construction);
                player.Matter -= construction * _settings.CompoundCost;
            }
        }
        foreach ((TurnHexagon key, Compound construction) in player.Constructions.Where(x => x.Key.Turn == _state.Turn).ToList())
        {
            player.Constructions.Remove(key);
            player.Compounds.Merge(key.Hexagon, construction);
            player.Report.CompletedConstructions.Add(key.Hexagon, construction);
            player.AvailableSpace += construction * _settings.ProvidedSpace;
        }
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
            Dictionary<string, Combatant> combatants = GetCombatantsOn(hexagon);
            if (combatants.Count < 2)
            {
                continue;
            }
            FightReport fight = Combat.Fight(_settings, combatants);
            _fights.Add(hexagon, fight);
            foreach ((string playerId, Army loss) in fight.Losses)
            {
                ApplyLosses(_players[playerId], hexagon, loss);
            }
        }
    }

    /// <summary>
    /// The upgrade levels come from the state at the start of the turn: a research completed
    /// this turn counts from the next turn on.
    /// </summary>
    private Dictionary<string, Combatant> GetCombatantsOn(Hexagon hexagon)
    {
        Dictionary<string, Combatant> combatants = [];
        foreach (string playerId in _state.PlayerIds)
        {
            if (_players[playerId].Armies.TryGetValue(hexagon, out var army))
            {
                combatants.Add(playerId, new Combatant()
                {
                    Army = army,
                    UpgradeLevels = _state.PlayerStates[playerId].UpgradeLevels
                });
            }
        }
        return combatants;
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
        var candidates = player.Constructions
            .Where(x => x.Key.Hexagon == hexagon)
            .OrderByDescending(x => x.Key.Turn)
            .ToList();
        foreach ((TurnHexagon key, Compound construction) in candidates)
        {
            foreach (Building building in EnumValues.Buildings)
            {
                if (dotLosses <= 0)
                {
                    return;
                }
                if (construction[building] <= 0)
                {
                    continue;
                }
                Compound canceled = Compound.FromBuilding(building, Math.Min(construction[building], dotLosses));
                dotLosses -= canceled[building];
                player.Constructions.Merge(key, -canceled);
                player.Report.CanceledConstructions.Merge(hexagon, canceled);
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
    /// Every destroyed building cancels one training that needs it and refunds the space,
    /// the earliest completion turn first.
    /// </summary>
    private void CancelTrainings(PlayerStateBuilder player, Hexagon hexagon, Compound destroyedCompound)
    {
        var candidates = player.Trainings
            .Where(x => x.Key.Hexagon == hexagon)
            .OrderBy(x => x.Key.Turn)
            .ToList();
        foreach (Building building in EnumValues.Buildings)
        {
            short destroyedCount = destroyedCompound[building];
            foreach ((TurnHexagon key, Army training) in candidates)
            {
                foreach (Unit unit in EnumValues.Units)
                {
                    if (destroyedCount <= 0 ||
                        training[unit] <= 0 ||
                        GameSettings.TrainingBuilding(unit) != building)
                    {
                        continue;
                    }
                    short canceledCount = (short)Math.Min(training[unit], destroyedCount);
                    destroyedCount -= canceledCount;
                    Army canceled = Army.FromUnit(unit, canceledCount);
                    player.UsedSpace -= canceled * _settings.RequiredSpace;
                    player.Trainings.Merge(key, -canceled);
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
}
