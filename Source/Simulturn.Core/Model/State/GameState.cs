using Simulturn.Core.Extensions;
using Simulturn.Core.Model.Commands;

namespace Simulturn.Core.Model.State;

public record GameState
{
    private static readonly ImmutableArray<Unit> _units = Enum.GetValues(typeof(Unit)).Cast<Unit>().ToImmutableArray();
    private static readonly ImmutableArray<Building> _buildings = Enum.GetValues(typeof(Building)).Cast<Building>().ToImmutableArray();
    public GameSettings GameSettings { get; init; }
    public ImmutableHashSet<Hexagon> Hexagons { get; init; }
    public HashSet<string> PlayerIds { get; init; }
    public ImmutableDictionary<string, PlayerState> PlayerStates { get; init; }
    public ImmutableDictionary<Hexagon, int> RemainingMatter { get; init; }

    public ushort Turn { get; init; }

    public GameState(GameSettings gameSettings)
    {
        GameSettings = gameSettings;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        RemainingMatter = GameSettings.HexagonSettings.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.Matter);
        PlayerStates = GameSettings.HexagonSettings.Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key, x => new PlayerState()
            {
                Armies = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialArmy),
                Compounds = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialCompound),
                AvailableSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialCompound * GameSettings.ProvidedSpace),
                UsedSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialArmy * GameSettings.RequiredSpace),
                Constructions = Constructinos.Empty,
                Trainings = Trainings.Empty,
                Matter = GameSettings.StartMatter,
                Researches = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>>.Empty,
                UpgradeLevels = GameSettings.StartUpgrades.ContainsKey(x.Key)
                    ? GameSettings.StartUpgrades[x.Key].GroupBy(x => x).ToImmutableDictionary(x => x.Key, x => Convert.ToByte(x.Count()))
                    : ImmutableDictionary<Upgrade, byte>.Empty
            });
        PlayerIds = PlayerStates.Keys.ToHashSet();
    }

    private GameState(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter)
    {
        GameSettings = gameSettings;
        PlayerIds = playerStates.Keys.ToHashSet();
        Turn = turn;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        RemainingMatter = remainingMatter;
        PlayerStates = playerStates;
    }

    public GameState NextTurn<TCommands>(IReadOnlyDictionary<string, TCommands> commands)
            where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        var remainingMatter = RemainingMatter.ToBuilder();
        var playerStates = PlayerStates.ToDictionary(x => x.Key, x => PlayerStateBuilder.FromPlayerState(x.Value));
        HashSet<Hexagon> potentialFightHexagons = [];
        foreach (string playerId in PlayerIds)
        {
            IReadOnlyDictionary<Hexagon, Command> commandsForPlayer = commands.GetOrDefault(playerId);
            PlayerStateBuilder playerStateBuilder = playerStates[playerId];
            var newConstructions = GetNewConstructions(commandsForPlayer);
            var newTrainings = GetNewTrainings(commandsForPlayer);
            var newResearches = GetNewResearches(commandsForPlayer, playerStateBuilder.UpgradeLevels);

            // Income
            foreach ((Hexagon hexagon, Army army) in playerStateBuilder.Armies)
            {
                if (!playerStateBuilder.Compounds.TryGetValue(hexagon, out var compound) || compound.Plane == 0)
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
                idleArmy = Army.Min(idleArmy, GameSettings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter);
                int hexagonIncome = GameSettings.Income * idleArmy;
                hexagonIncome = Math.Min(hexagonIncome, remainingMatter[hexagon]);
                remainingMatter[hexagon] -= hexagonIncome;
                playerStateBuilder.Matter += hexagonIncome;
            }

            // Trainigs
            playerStateBuilder.Trainings.Merge(newTrainings);
            if (playerStateBuilder.Trainings.TryGetValue(Turn, out var trainings))
            {
                playerStateBuilder.Armies.Merge(trainings);
                playerStateBuilder.UsedSpace += trainings.Values.Sum(army => army * GameSettings.RequiredSpace);
            }
            playerStateBuilder.Matter -= newTrainings.Sum(x => x.Value.Values.Sum(training => training * GameSettings.ArmyCost));

            // Constructions
            playerStateBuilder.Constructions.Merge(newConstructions);
            if (playerStateBuilder.Constructions.TryGetValue(Turn, out var construction))
            {
                playerStateBuilder.Compounds.Merge(construction);
                playerStateBuilder.AvailableSpace += construction.Values.Sum(compound => compound * GameSettings.ProvidedSpace);
            }
            playerStateBuilder.Matter -= newConstructions.Sum(x => x.Value.Values.Sum(compound => compound * GameSettings.CompoundCost));

            // Researches
            playerStateBuilder.Researches.MergeOrOverwrite(newResearches);
            if (playerStateBuilder.Researches.TryGetValue(Turn, out var researches))
            {
                foreach ((var hexagon, var upgrade) in researches)
                {
                    if (!playerStateBuilder.UpgradeLevels.TryGetValue(upgrade, out byte level))
                    {
                        playerStateBuilder.UpgradeLevels.Add(upgrade, 0);
                    }
                    playerStateBuilder.UpgradeLevels[upgrade]++;
                }
            }

            // Movements
            foreach (var hexagon in commandsForPlayer.Keys)
            {
                for (int i = 0; i < commands[playerId][hexagon].MovementCommands.Length; i++)
                {
                    var movement = commands[playerId][hexagon].MovementCommands[i];
                    if (movement.Army.IsEmpty)
                    {
                        continue;
                    }
                    playerStateBuilder.Armies.Merge(hexagon, -movement.Army);
                    playerStateBuilder.Armies.Merge(movement.Destination, movement.Army);
                    potentialFightHexagons.Add(movement.Destination);
                }
            }
        }

        // Fights
        foreach (var hexagon in potentialFightHexagons)
        {
            List<(string playerId, Army army)> armies = [];
            foreach (var playerId in PlayerIds)
            {
                if (playerStates[playerId].Armies.TryGetValue(hexagon, out var army))
                {
                    armies.Add((playerId, army));
                }
            }
            if (armies.Count < 2)
            {
                continue;
            }
            Dictionary<string, Army> losses = [];
            for (var i = 0; i < armies.Count - 1; i++)
            {
                for (int j = i + 1; j < armies.Count; j++)
                {
                    (Army lossesPlayerI, Army lossesPlayerJ) = Fight(armies[i].playerId,
                        armies[i].army,
                        armies[j].playerId,
                        armies[j].army);
                    losses.Merge(armies[i].playerId, lossesPlayerI);
                    losses.Merge(armies[j].playerId, lossesPlayerJ);
                }
            }
            foreach (var playerId in losses.Keys)
            {
                playerStates[playerId].Armies.Merge(hexagon, -losses[playerId]);
                playerStates[playerId].UsedSpace -= losses[playerId] * GameSettings.RequiredSpace;
            }
        }

        // Destroy Buildings
        foreach (var hexagon in Hexagons)
        {
            foreach (var armyPlayerId in PlayerIds)
            {
                PlayerStateBuilder playerState = playerStates[armyPlayerId];
                foreach (var compoundPlayerId in PlayerIds)
                {
                    if (armyPlayerId == compoundPlayerId)
                    {
                        continue;
                    }
                    if (playerStates[armyPlayerId].Armies.TryGetValue(hexagon, out var army) && !army.IsEmpty &&
                        playerStates[compoundPlayerId].Compounds.TryGetValue(hexagon, out var compound) && !compound.IsEmpty)
                    {
                        double damage = GameSettings.StructureDamage * army;
                        var buildings = _buildings.OrderBy(x => GameSettings.Armor[x]);
                        var destroyedCompound = Compound.Empty;
                        foreach (var building in buildings)
                        {
                            var destroyedBuildings = (short)Math.Min(compound[building], Math.Floor(damage / (double)GameSettings.Armor[building]));
                            if (destroyedBuildings <= 0)
                            {
                                continue;
                            }
                            damage -= destroyedBuildings * GameSettings.Armor[building];
                            destroyedCompound = destroyedCompound + Compound.FromBuilding(building, destroyedBuildings);
                        }
                        if (destroyedCompound.IsEmpty)
                        {
                            continue;
                        }
                        playerStates[compoundPlayerId].Compounds.Merge(hexagon, -destroyedCompound);
                        playerStates[compoundPlayerId].AvailableSpace -= destroyedCompound * GameSettings.ProvidedSpace;
                    }
                }
            }
        }

        if (Turn == ushort.MaxValue)
        {
            throw new InvalidOperationException("Max turn limit reached.");
        }
        return new GameState(GameSettings,
            (ushort)(1 + Turn),
            playerStates.ToImmutableDictionary(x => x.Key, x => x.Value.ToPlayerState()),
            remainingMatter.ToImmutableDictionary());
    }

    private (Army lossesPlayer0, Army lossesPlayer1) Fight(string player1Id, Army army1, string player2Id, Army army2)
    {
        Army player1Upgrades = PlayerStates[player1Id].ExponentBonusFromUpgrades(GameSettings);
        Army player2Upgrades = PlayerStates[player2Id].ExponentBonusFromUpgrades(GameSettings);
        var army1Strength = army1.GetStrengthOver(army2, GameSettings.FightExponent + player1Upgrades);
        var army2Strength = army2.GetStrengthOver(army1, GameSettings.FightExponent + player2Upgrades);
        Army arm1Losses;
        Army arm2Losses;
        if (army1Strength > army2Strength)
        {
            var fraction = army2Strength / (double)army1Strength;
            arm1Losses = army1.MultiplyAndRoundUp(fraction);
            arm2Losses = army2;
        }
        else if (army1Strength == 0 && army2Strength == 0)
        {
            return (army1, army2);
        }
        else
        {
            var fraction = army1Strength / army2Strength;
            arm2Losses = army2.MultiplyAndRoundUp(fraction);
            arm1Losses = army1;
        }
        return (arm1Losses, arm2Losses);
    }

    private ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>.Builder>.Builder GetNewTrainings(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>.Builder>.Builder newTrainings = ImmutableDictionary.CreateBuilder<ushort, ImmutableDictionary<Hexagon, Army>.Builder>();
        foreach ((var hexagon, var command) in commands.Where(x => !x.Value.Training.IsEmpty))
        {
            Army training = command.Training;
            foreach (var unit in _units)
            {
                if (training[unit] <= 0)
                {
                    continue;
                }
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(Turn + GameSettings.TrainingDuration[unit] - 1);
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

    private ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>.Builder GetNewConstructions(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>.Builder newConstructions = ImmutableDictionary.CreateBuilder<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>();
        foreach ((var hexagon, var command) in commands.Where(x => !x.Value.Construction.IsEmpty))
        {
            Compound construction = command.Construction;
            foreach (var building in _buildings)
            {
                if (construction[building] <= 0)
                {
                    continue;
                }
                // Subtract 1 to account for the current turn, the turn increments after all game state logic is completed
                ushort completionTurn = (ushort)(Turn + GameSettings.ConstructionDuration[building] - 1);
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

    private ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>.Builder>.Builder GetNewResearches(IReadOnlyDictionary<Hexagon, Command> commands,
        ImmutableDictionary<Upgrade, byte>.Builder upgradeLevels)
    {
        ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>.Builder>.Builder newResearches = ImmutableDictionary.CreateBuilder<ushort, ImmutableDictionary<Hexagon, Upgrade>.Builder>();
        foreach ((var hexagon, var command) in commands.Where(x => x.Value.Upgrade is not null))
        {
            Upgrade upgrade = command.Upgrade!.Value;
            byte currentLevel = upgradeLevels.GetValueOrDefault(upgrade, 0);
            ushort completionTurn = (ushort)(Turn + GameSettings.Upgrades[upgrade][currentLevel].Duration - 1);

            if (!newResearches.TryGetValue(completionTurn, out var turnDict))
            {
                turnDict = ImmutableDictionary.CreateBuilder<Hexagon, Upgrade>();
                newResearches.Add(completionTurn, turnDict);
            }
            turnDict[hexagon] = upgrade;

        }
        return newResearches;
    }
}