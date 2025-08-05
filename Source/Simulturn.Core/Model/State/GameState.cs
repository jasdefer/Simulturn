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
    public ImmutablePlayerHexagonCompound PlayerCompounds { get; init; }
    public ImmutablePlayerHexagonArmies PlayerArmies { get; init; }
    public ImmutableDictionary<ushort, ImmutablePlayerHexagonArmies> Trainings { get; init; }
    public ImmutableDictionary<ushort, ImmutablePlayerHexagonCompound> Constructions { get; init; }
    public ImmutableDictionary<Hexagon, int> RemainingMatter { get; init; }

    public ushort Turn { get; init; }

    public GameState(GameSettings gameSettings)
    {
        GameSettings = gameSettings;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        PlayerArmies = GameSettings.HexagonSettings
            .Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key,
                x => x.ToImmutableDictionary(y => y.Key,
                x => x.Value.PlayerInitialization!.Value.InitialArmy));
        PlayerCompounds = GameSettings.HexagonSettings
            .Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key,
                x => x.ToImmutableDictionary(y => y.Key,
                x => x.Value.PlayerInitialization!.Value.InitialCompound));
        RemainingMatter = GameSettings.HexagonSettings.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.Matter);
        PlayerStates = PlayerArmies.Keys
            .ToImmutableDictionary(
                x => x,
                playerId => new PlayerState()
                {
                    Matter = GameSettings.StartMatter,
                    UsedSpace = PlayerArmies[playerId].Sum(army => army.Value * GameSettings.RequiredSpace),
                    AvailableSpace = PlayerCompounds[playerId].Sum(compound => compound.Value * GameSettings.ProvidedSpace)
                }
            );
        PlayerIds = PlayerStates.Keys.ToHashSet();
        Trainings = ImmutableDictionary<ushort, ImmutablePlayerHexagonArmies>.Empty;
        Constructions = ImmutableDictionary<ushort, ImmutablePlayerHexagonCompound>.Empty;
    }

    private GameState(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutablePlayerHexagonArmies playerArmies,
        ImmutablePlayerHexagonCompound playerCompounds,
        ImmutableDictionary<ushort, ImmutablePlayerHexagonArmies> trainings,
        ImmutableDictionary<ushort, ImmutablePlayerHexagonCompound> constructions,
        ImmutableDictionary<Hexagon, int> remainingMatter)
    {
        GameSettings = gameSettings;
        Turn = turn;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        PlayerArmies = playerArmies;
        PlayerCompounds = playerCompounds;
        Trainings = trainings;
        Constructions = constructions;
        RemainingMatter = remainingMatter;
        PlayerStates = playerStates;
    }

    public bool IsValid(string playerId, Dictionary<Hexagon, Command> commands)
    {
        int requiredMatter = commands.Values.Sum(command => command.Training * GameSettings.ArmyCost + command.Construction * GameSettings.CompoundCost);
        if (requiredMatter > PlayerStates[playerId].Matter)
        {
            return false;
        }
        int requiredSpace = commands.Values.Sum(command => command.Training * GameSettings.RequiredSpace);
        if (requiredSpace + PlayerStates[playerId].UsedSpace > PlayerStates[playerId].AvailableSpace)
        {
            return false;
        }

        foreach ((Hexagon hexagon, Command command) in commands)
        {
            int newConstructionSides = command.Construction.Sum();
            int existingConstructionSides = Constructions
                .Where(x => x.Key >= Turn)
                .Sum(x => x.Value[playerId][hexagon].Sum());
            if (newConstructionSides + existingConstructionSides > PlayerArmies[playerId][hexagon].Dot)
            {
                return false;
            }

            Army existingTrainings = Trainings
                .Where(x => x.Key >= Turn)
                .Select(x => x.Value[playerId][hexagon])
                .Sum();
        }

        return true;
    }

    public GameState NextTurn(PlayerHexagonCommands commands)
    {
        // Income
        var remainingMatter = RemainingMatter.ToBuilder();
        var playerStates = PlayerStates.ToBuilder();
        foreach (var playerId in PlayerIds)
        {
            int income = 0;
            foreach ((Hexagon hexagon, Army army) in PlayerArmies[playerId])
            {
                if (!PlayerCompounds[playerId].TryGetValue(hexagon, out var compound) || compound.Plane == 0)
                {
                    continue;
                }
                Compound upcomingConstructions = Constructions.SumConstructions(Turn, playerId, hexagon);
                var idleArmy = army with
                {
                    Dot = (short)Math.Max(0, army.Dot - upcomingConstructions.Sum())
                };
                idleArmy = Army.Min(idleArmy, GameSettings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter);
                int hexagonIncome = GameSettings.Income * idleArmy;
                hexagonIncome = Math.Min(hexagonIncome, remainingMatter[hexagon]);
                remainingMatter[hexagon] -= hexagonIncome;
                income += hexagonIncome;
            }
            int spendMatter = 0;
            if (commands.TryGetValue(playerId, out var playerCommands))
            {
                spendMatter = playerCommands.Sum(x => x.Value.Construction * GameSettings.CompoundCost +
                    x.Value.Training * GameSettings.ArmyCost);
            }

            playerStates[playerId] = playerStates[playerId] with
            {
                Matter = playerStates[playerId].Matter + income - spendMatter,
            };
        }

        // Trainigs
        Dictionary<ushort, PlayerHexagonArmies> newTrainings = GetNewTrainings(commands);
        PlayerHexagonArmies playerArmies = PlayerArmies.Copy();
        if (newTrainings.TryGetValue(Turn, out var trainings))
        {
            playerArmies.MergeArmies(trainings);
            foreach (var playerId in trainings.Keys)
            {
                int usedSpace = playerStates[playerId].UsedSpace + trainings[playerId].Values.Sum(army => army * GameSettings.RequiredSpace);
                playerStates[playerId] = playerStates[playerId] with
                {
                    UsedSpace = usedSpace,
                };
            }
        }

        // Constructions
        Dictionary<ushort, PlayerHexagonCompound> newConstructions = GetNewConstructions(commands);
        var playerCompounds = PlayerCompounds.Copy();
        if (newConstructions.TryGetValue(Turn, out var constructions))
        {
            playerCompounds.MergeCompounds(constructions);
            foreach (var playerId in constructions.Keys)
            {
                int availableSpace = playerStates[playerId].AvailableSpace + constructions[playerId].Values.Sum(construction => construction * GameSettings.ProvidedSpace);
                playerStates[playerId] = playerStates[playerId] with
                {
                    AvailableSpace = availableSpace
                };
            }
        }

        // Movements
        HashSet<Hexagon> potentialFightHexagons = [];
        foreach (var playerId in commands.Keys)
        {
            foreach (var hexagon in commands[playerId].Keys)
            {
                var movement = commands[playerId][hexagon].MovementCommand;
                if (movement.Army.IsEmpty)
                {
                    continue;
                }
                playerArmies[playerId].Merge(hexagon, -movement.Army);
                playerArmies[playerId].Merge(movement.Destination, movement.Army);
                potentialFightHexagons.Add(movement.Destination);
            }
        }

        // Fights
        foreach (var hexagon in potentialFightHexagons)
        {
            List<(string playerId, Army army)> armies = [];
            foreach (var playerId in playerArmies.Keys)
            {
                if (playerArmies[playerId].TryGetValue(hexagon, out var army))
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
                playerArmies[playerId].Merge(hexagon, -losses[playerId]);
                playerStates[playerId] = playerStates[playerId] with
                {
                    UsedSpace = playerStates[playerId].UsedSpace - losses[playerId] * GameSettings.RequiredSpace
                };
            }
        }

        // Destroy Buildings
        foreach (var hexagon in Hexagons)
        {
            foreach (var armyPlayerId in playerArmies.Keys)
            {
                foreach (var compoundPlayerId in playerCompounds.Keys)
                {
                    if (armyPlayerId == compoundPlayerId)
                    {
                        continue;
                    }
                    if (playerArmies[armyPlayerId].TryGetValue(hexagon, out var army) && !army.IsEmpty &&
                        playerCompounds[compoundPlayerId].TryGetValue(hexagon, out var compound) && !compound.IsEmpty)
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
                        playerCompounds[compoundPlayerId].Merge(hexagon, -destroyedCompound);
                        playerStates[compoundPlayerId] = playerStates[compoundPlayerId] with
                        {
                            AvailableSpace = playerStates[compoundPlayerId].AvailableSpace - destroyedCompound * GameSettings.ProvidedSpace
                        };
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
            playerStates.ToImmutableDictionary(),
            playerArmies.ToImmutable(),
            playerCompounds.ToImmutable(),
            newTrainings.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutable()),
            newConstructions.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutable()),
            remainingMatter.ToImmutableDictionary());
    }

    private (Army lossesPlayer0, Army lossesPlayer1) Fight(string player1Id, Army army1, string player2Id, Army army2)
    {
        var army1Strength = army1.GetStrengthOver(army2, GameSettings.FightExponent);
        var army2Strength = army2.GetStrengthOver(army1, GameSettings.FightExponent);
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
            return (Army.Empty, Army.Empty);
        }
        else
        {
            var fraction = army1Strength / army2Strength;
            arm2Losses = army2.MultiplyAndRoundUp(fraction);
            arm1Losses = army1;
        }
        return (arm1Losses, arm2Losses);
    }

    public GameState GetFromThePerspectiveOf(string player)
    {
        throw new NotImplementedException();
    }

    private Dictionary<ushort, PlayerHexagonArmies> GetNewTrainings(PlayerHexagonCommands commands)
    {
        Dictionary<ushort, PlayerHexagonArmies> newTrainings = [];
        foreach (var playerId in commands.Keys)
        {
            foreach (var hexagon in commands[playerId].Keys)
            {
                Army training = commands[playerId][hexagon].Training;
                foreach (var unit in _units)
                {
                    if (training[unit] <= 0)
                    {
                        continue;
                    }
                    ushort completionTurn = (ushort)(Turn + GameSettings.TrainingDuration[unit]);
                    if (!newTrainings.TryGetValue(completionTurn, out var turnDict))
                    {
                        turnDict ??= [];
                        newTrainings.Add(completionTurn, turnDict);
                    }
                    if (!turnDict.TryGetValue(playerId, out var hexagonDict))
                    {
                        hexagonDict ??= [];
                        turnDict.Add(playerId, hexagonDict);
                    }
                    var trainedUnit = Army.FromUnit(unit, training[unit]);
                    hexagonDict.Merge(hexagon, trainedUnit);
                }
            }
        }
        foreach (var turn in Trainings.Keys)
        {
            if (!newTrainings.TryGetValue(turn, out var turnDict))
            {
                turnDict = [];
                newTrainings.Add(turn, turnDict);
            }
            foreach (var playerId in Trainings[turn].Keys)
            {
                if (!turnDict.TryGetValue(playerId, out var playerDict))
                {
                    playerDict = [];
                    turnDict.Add(playerId, playerDict);
                }
                foreach (var hexagon in Trainings[turn][playerId].Keys)
                {
                    playerDict.Merge(hexagon, Trainings[turn][playerId][hexagon]);
                }
            }
        }

        return newTrainings;
    }

    private Dictionary<ushort, PlayerHexagonCompound> GetNewConstructions(PlayerHexagonCommands commands)
    {
        Dictionary<ushort, PlayerHexagonCompound> newConstructions = [];
        foreach (var playerId in commands.Keys)
        {
            foreach (var hexagon in commands[playerId].Keys)
            {
                Compound construction = commands[playerId][hexagon].Construction;
                foreach (var building in _buildings)
                {
                    if (construction[building] <= 0)
                    {
                        continue;
                    }
                    ushort completionTurn = (ushort)(Turn + GameSettings.ConstructionDuration[building]);
                    if (!newConstructions.TryGetValue(completionTurn, out var turnDict))
                    {
                        turnDict ??= [];
                        newConstructions.Add(completionTurn, turnDict);
                    }
                    if (!turnDict.TryGetValue(playerId, out var hexagonDict))
                    {
                        hexagonDict ??= [];
                        turnDict.Add(playerId, hexagonDict);
                    }
                    var constructedBuilding = Compound.FromBuilding(building, construction[building]);
                    hexagonDict.Merge(hexagon, constructedBuilding);
                }
            }
        }
        foreach (var turn in Constructions.Keys)
        {
            if (!newConstructions.TryGetValue(turn, out var turnDict))
            {
                turnDict = [];
                newConstructions.Add(turn, turnDict);
            }
            foreach (var playerId in Constructions[turn].Keys)
            {
                if (!turnDict.TryGetValue(playerId, out var playerDict))
                {
                    playerDict = [];
                    turnDict.Add(playerId, playerDict);
                }
                foreach (var hexagon in Constructions[turn][playerId].Keys)
                {
                    playerDict.Merge(hexagon, Constructions[turn][playerId][hexagon]);
                }
            }
        }

        return newConstructions;
    }
}