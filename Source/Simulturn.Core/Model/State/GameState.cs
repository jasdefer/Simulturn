using Simulturn.Core.Extensions;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State.StateValidation;

namespace Simulturn.Core.Model.State;

public record GameState
{
    private static readonly ImmutableArray<Unit> _units = Enum.GetValues<Unit>().Cast<Unit>().ToImmutableArray();
    private static readonly ImmutableArray<Building> _buildings = Enum.GetValues<Building>().Cast<Building>().ToImmutableArray();
    public GameSettings GameSettings { get; init; }
    public ImmutableHashSet<Hexagon> Hexagons { get; init; }
    public HashSet<string> PlayerIds { get; init; }
    public ImmutableDictionary<string, PlayerState> PlayerStates { get; init; }
    public ImmutableDictionary<Hexagon, int> RemainingMatter { get; init; }
    public bool IsGameOver => PlayerStates.Where(x => x.Value.Compounds.Any(compound => !compound.Value.IsEmpty)).Count() <= 1;

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
            .ToImmutableDictionary(x => x.Key, x =>
            {
                var armies = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialArmy);
                return new PlayerState()
                {
                    Armies = armies,
                    Compounds = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialCompound),
                    AvailableSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialCompound * GameSettings.ProvidedSpace),
                    UsedSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialArmy * GameSettings.RequiredSpace),
                    Constructions = Constructions.Empty,
                    Trainings = Trainings.Empty,
                    Matter = GameSettings.StartMatter,
                    Losses = ImmutableDictionary<Hexagon, Army>.Empty,
                    Researches = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>>.Empty,
                    Visibilities = PlayerStateBuilder.GetVisibility(armies, Hexagons, gameSettings.PartialVisibilityRange, gameSettings.VisibilityRange),
                    UpgradeLevels = GameSettings.StartUpgrades.ContainsKey(x.Key)
                        ? GameSettings.StartUpgrades[x.Key].GroupBy(x => x).ToImmutableDictionary(x => x.Key, x => Convert.ToByte(x.Count()))
                        : ImmutableDictionary<Upgrade, byte>.Empty
                };
            });
        PlayerStates = FogOfWar.UpdateMemories(PlayerStates, RemainingMatter, Turn);
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
            playerStateBuilder.UsedSpace += newTrainings.Values.SelectMany(x => x.Values).Sum(army => army * GameSettings.RequiredSpace);
            playerStateBuilder.Trainings.Merge(newTrainings);
            if (playerStateBuilder.Trainings.TryGetValue(Turn, out var trainings))
            {
                playerStateBuilder.Armies.Merge(trainings);
                potentialFightHexagons.UnionWith(trainings.Keys); // The army might be trained on a hexagon with an enemy army
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
            foreach ((string playerId, Army loss) in losses)
            {
                playerStates[playerId].Losses.Add(hexagon, loss);
                playerStates[playerId].Armies.Merge(hexagon, -loss);
                playerStates[playerId].UsedSpace -= loss * GameSettings.RequiredSpace;

                short dotLosses = loss.Dot;
                if (dotLosses <= 0)
                {
                    continue;
                }

                // Stop constructions for each lost dot
                Compound constructions = playerStates[playerId].Constructions
                    .Where(x => x.Key >= Turn)
                    .Select(x => x.Value.GetValueOrDefault(hexagon))
                    .Sum();
                var canceledConstructionCandidates = playerStates[playerId]
                    .Constructions
                    .Where(x => x.Key >= Turn)
                    .OrderByDescending(x => x.Key);
                foreach ((ushort turn, var constructionForTurn) in canceledConstructionCandidates)
                {
                    if (!constructionForTurn.TryGetValue(hexagon, out var construction) || construction.IsEmpty)
                    {
                        continue;
                    }
                    Compound newConstruction = construction;
                    foreach (var building in _buildings)
                    {
                        if (newConstruction[building] <= 0)
                        {
                            continue;
                        }
                        Compound cancel = Compound.FromBuilding(building, Math.Min(newConstruction[building], dotLosses));
                        dotLosses -= cancel[building];
                        newConstruction -= cancel;
                        if (dotLosses <= 0)
                        {
                            break;
                        }
                    }
                    if (newConstruction.IsEmpty)
                    {
                        playerStates[playerId].Constructions[turn].Remove(hexagon);
                        if (playerStates[playerId].Constructions[turn].Count == 0)
                        {
                            playerStates[playerId].Constructions.Remove(turn);
                        }
                    }
                    else
                    {
                        playerStates[playerId].Constructions[turn][hexagon] = newConstruction;
                    }
                }
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

                        // Cancel trainings for each destroyed building
                        foreach (var building in _buildings)
                        {
                            short destroyedCount = destroyedCompound[building];
                            if (destroyedCount <= 0)
                            {
                                continue;
                            }
                            var cancelCandidates = playerStates[compoundPlayerId].Trainings.Where(x => x.Key >= Turn);
                            foreach ((ushort turn, var candidate) in cancelCandidates)
                            {
                                if (destroyedCount <= 0 || !candidate.TryGetValue(hexagon, out Army training) || training.IsEmpty)
                                {
                                    continue;
                                }
                                foreach (Unit unit in _units)
                                {
                                    if (destroyedCount <= 0 ||
                                        training[unit] <= 0 ||
                                        GameSettings.TrainingBuildingPerUnit[unit] != building)
                                    {
                                        continue;
                                    }
                                    short canceledCount = (short)Math.Min(training[unit], destroyedCount);
                                    playerStates[compoundPlayerId].UsedSpace -= canceledCount * GameSettings.RequiredSpace[unit];
                                    destroyedCount -= canceledCount;
                                    playerStates[compoundPlayerId].Trainings[turn].Merge(hexagon, -Army.FromUnit(unit, canceledCount));
                                }
                            }
                        }
                    }
                }
            }
        }

        if (Turn == ushort.MaxValue)
        {
            throw new InvalidOperationException("Max turn limit reached.");
        }
        ushort nextTurn = (ushort)(1 + Turn);
        var newRemainingMatter = remainingMatter.ToImmutableDictionary();
        var newPlayerStates = playerStates.ToImmutableDictionary(x => x.Key, x => x.Value.ToPlayerState(GameSettings));
        newPlayerStates = FogOfWar.UpdateMemories(newPlayerStates, newRemainingMatter, nextTurn);
        return new GameState(GameSettings,
            nextTurn,
            newPlayerStates,
            newRemainingMatter);
    }

    /// <summary>
    /// Builds the partially informed view of the game for a single player.
    /// Clients and AI players must only receive this view, never the full game state.
    /// </summary>
    public PlayerGameState GetPlayerGameState(string playerId)
    {
        return FogOfWar.GetPlayerGameState(this, playerId);
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

    public IEnumerable<IStateValidation> IsValid()
    {
        // Remaining Matter
        foreach ((var hexagon, int remainingMatter) in RemainingMatter)
        {
            if (remainingMatter < 0)
            {
                yield return new NegativeRemainingMatter(hexagon, remainingMatter);
            }
            if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
            {
                yield return new HexagonMissingInSettings(hexagon);
            }
        }

        // Player States
        foreach ((var playerId, var playerState) in PlayerStates)
        {
            if (!PlayerIds.Contains(playerId))
            {
                yield return new InvalidPlayerId(playerId);
            }

            // Armies
            foreach ((var hexagon, var army) in playerState.Armies)
            {
                if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
                {
                    yield return new HexagonMissingInSettings(hexagon);
                }
                foreach (var unit in _units)
                {
                    if (army[unit] < 0)
                    {
                        yield return new NegativeUnitCount(playerId, hexagon, unit, army[unit]);
                    }
                }
            }

            // Compounds
            foreach ((var hexagon, var compound) in playerState.Compounds)
            {
                if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
                {
                    yield return new HexagonMissingInSettings(hexagon);
                }
                foreach (var building in _buildings)
                {
                    if (compound[building] < 0)
                    {
                        yield return new NegativeBuildingCount(playerId, hexagon, building, compound[building]);
                    }
                }
            }

            // Trainings
            foreach ((ushort turn, var trainings) in playerState.Trainings.Where(x => x.Key >= Turn))
            {
                foreach ((Hexagon hexagon, Army training) in trainings)
                {
                    if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
                    {
                        yield return new HexagonMissingInSettings(hexagon);
                    }
                    Compound compound = playerState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
                    foreach (var unit in _units)
                    {
                        if (training[unit] < 0)
                        {
                            yield return new NegativeTrainingCount(playerId, hexagon, unit, training[unit]);
                        }
                        if (training[unit] > compound[GameSettings.TrainingBuildingPerUnit[unit]])
                        {
                            yield return new MissingBuildingForTraining(playerId, hexagon, unit, training[unit], compound[GameSettings.TrainingBuildingPerUnit[unit]]);
                        }
                    }
                }
            }

            // Constructions
            foreach ((ushort turn, var constructions) in playerState.Constructions.Where(x => x.Key >= Turn))
            {
                foreach ((Hexagon hexagon, Compound construction) in constructions)
                {
                    if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
                    {
                        yield return new HexagonMissingInSettings(hexagon);
                    }
                    short dotCount = playerState.Armies.GetValueOrDefault(hexagon, Army.Empty).Dot;
                    foreach (var building in _buildings)
                    {
                        if (construction[building] < 0)
                        {
                            yield return new NegativeConstructionCount(playerId, hexagon, building, construction[building]);
                        }
                        if (construction.Sum() > dotCount)
                        {
                            yield return new MissingDotsForConstruction(playerId, hexagon, construction, dotCount);
                        }
                    }
                }
            }

            // Researches
            foreach ((ushort turn, var researches) in playerState.Researches.Where(x => x.Key >= Turn))
            {
                foreach ((Hexagon hexagon, Upgrade upgrade) in researches)
                {
                    if (!GameSettings.HexagonSettings.ContainsKey(hexagon))
                    {
                        yield return new HexagonMissingInSettings(hexagon);
                    }
                    if (!playerState.Armies.TryGetValue(hexagon, out var army) || army.Dot <= 0)
                    {
                        yield return new UpgradeRequiresDot(playerId, hexagon, upgrade);
                    }

                    byte currentLevel = playerState.UpgradeLevels.GetValueOrDefault(upgrade);
                    int availableUpgrades = GameSettings.HexagonSettings[hexagon]
                        .ResearchableUpgrades.Where(x => x == upgrade)
                        .Count();
                    if (currentLevel > availableUpgrades)
                    {
                        yield return new UpgradeExceedsAvailableLevel(playerId, hexagon, upgrade, currentLevel, availableUpgrades);
                    }
                }
            }
        }
    }

    public IEnumerable<ICommandValidation> Validate(string playerId, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        if (!PlayerIds.Contains(playerId))
        {
            yield return new InvalidPlayerId(playerId);
            yield break;
        }

        var playerState = PlayerStates[playerId];

        // Space
        int additionalSpace = commands.Sum(command => command.Value.Training * GameSettings.RequiredSpace);
        if (commands.Any(x => x.Value.Training.Total > 0) &&
            PlayerStates[playerId].UsedSpace + additionalSpace > PlayerStates[playerId].AvailableSpace)
        {
            // Only check the space, if there is a training. A player can have more space than available, but not train in such a case.
            yield return new InsufficientSpace(playerId, PlayerStates[playerId].UsedSpace + additionalSpace, PlayerStates[playerId].AvailableSpace);
        }

        int researchCost = 0;
        foreach ((var hexagon, var command) in commands)
        {
            Compound compound = playerState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
            // Training has enough compounds
            if (!command.Training.IsEmpty)
            {
                Army trainings = playerState.Trainings
                    .Where(x => x.Key >= Turn)
                    .Select(x => x.Value.GetValueOrDefault(hexagon))
                    .Sum();
                trainings += command.Training;
                foreach (var unit in _units)
                {
                    short trainedUnits = trainings[unit];
                    short availableBuildings = compound[GameSettings.TrainingBuildingPerUnit[unit]];
                    if (trainedUnits > availableBuildings)
                    {
                        yield return new MissingBuildingForTraining(playerId, hexagon, unit, trainedUnits, availableBuildings);
                    }
                }
            }

            // Constructions have enough dots (workers)
            if (!command.Construction.IsEmpty)
            {
                Compound constructions = playerState.Constructions
                    .Where(x => x.Key >= Turn)
                    .Select(x => x.Value.GetValueOrDefault(hexagon))
                    .Sum();
                constructions += command.Construction;
                if (constructions.Sum() > playerState.Armies.GetValueOrDefault(hexagon)[Unit.Dot])
                {
                    yield return new MissingDotsForConstruction(playerId, hexagon, constructions, playerState.Armies.GetValueOrDefault(hexagon)[Unit.Dot]);
                }
            }

            // Movement has enough armies
            Army departingArmy = command.MovementCommands.Select(x => x.Army).Sum();
            Army availableArmy = playerState.Armies.GetValueOrDefault(hexagon);
            foreach (var unit in _units)
            {
                if (departingArmy[unit] > availableArmy[unit])
                {
                    yield return new MissingArmyForMovement(playerId, hexagon, unit, departingArmy[unit], availableArmy[unit]);
                }
            }

            // Upgrades are valid
            if (command.Upgrade is not null)
            {
                byte currentLevel = playerState.UpgradeLevels.GetValueOrDefault(command.Upgrade.Value);
                int availableUpgrades = GameSettings.HexagonSettings[hexagon]
                    .ResearchableUpgrades.Where(x => x == command.Upgrade)
                    .Count();
                if (currentLevel >= availableUpgrades)
                {
                    yield return new UpgradeExceedsAvailableLevel(playerId, hexagon, command.Upgrade.Value, currentLevel, availableUpgrades);
                }
                else
                {
                    researchCost += GameSettings.Upgrades[command.Upgrade.Value][currentLevel].Cost; // a level of 1 means the first upgrade, which is at index 0
                }

                if (!playerState.Armies.TryGetValue(hexagon, out var army) || army.Dot <= 0)
                {
                    yield return new UpgradeRequiresDot(playerId, hexagon, command.Upgrade.Value);
                }
            }
        }

        // Matter
        int trainingCost = commands.Sum(command => command.Value.Training * GameSettings.ArmyCost);
        int constructionCost = commands.Sum(command => command.Value.Construction * GameSettings.CompoundCost);
        int totalCost = trainingCost + constructionCost + researchCost;
        if (totalCost > PlayerStates[playerId].Matter)
        {
            yield return new InsufficientMatter(playerId, totalCost, PlayerStates[playerId].Matter);
        }
    }
}