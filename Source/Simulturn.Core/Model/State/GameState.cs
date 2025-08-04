using Simulturn.Core.Extensions;
using Simulturn.Core.Model.Commands;

namespace Simulturn.Core.Model.State;

public record GameState
{
    private static readonly ImmutableArray<Unit> _units = Enum.GetValues(typeof(Unit)).Cast<Unit>().ToImmutableArray();
    private static readonly ImmutableArray<Building> _buildings = Enum.GetValues(typeof(Building)).Cast<Building>().ToImmutableArray();
    private readonly ushort _turn = 0;
    private readonly GameSettings _gameSettings;
    private readonly ImmutableHashSet<Hexagon> _hexagons;
    private readonly ImmutableDictionary<string, PlayerState> _playerStates;
    private readonly ImmutablePlayerHexagonCompound _playerCompounds;
    private readonly ImmutablePlayerHexagonArmies _playerArmies;
    private readonly ImmutableDictionary<ushort, ImmutablePlayerHexagonArmies> _trainings;
    private readonly ImmutableDictionary<ushort, ImmutablePlayerHexagonCompound> _constructions;
    private readonly ImmutableDictionary<Hexagon, int> _remainingMatter;

    public GameState(GameSettings gameSettings)
    {
        _gameSettings = gameSettings;
        _hexagons = _gameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        _playerArmies = _gameSettings.HexagonSettings
            .Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key,
                x => x.ToImmutableDictionary(y => y.Key,
                x => x.Value.PlayerInitialization!.Value.InitialArmy));
        _playerCompounds = _gameSettings.HexagonSettings
            .Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key,
                x => x.ToImmutableDictionary(y => y.Key,
                x => x.Value.PlayerInitialization!.Value.InitialCompound));
        _remainingMatter = _gameSettings.HexagonSettings.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.Matter);
        _playerStates = _playerArmies.Keys
            .ToImmutableDictionary(
                x => x,
                playerId => new PlayerState(
                    _gameSettings.StartMatter,
                    _playerArmies[playerId].Sum(army => army.Value * _gameSettings.RequiredSpace),
                    _playerCompounds[playerId].Sum(compound => compound.Value * _gameSettings.ProvidedSpace))
            );
        _trainings = ImmutableDictionary<ushort, ImmutablePlayerHexagonArmies>.Empty;
        _constructions = ImmutableDictionary<ushort, ImmutablePlayerHexagonCompound>.Empty;
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
        _gameSettings = gameSettings;
        _turn = turn;
        _hexagons = _gameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        _playerArmies = playerArmies;
        _playerCompounds = playerCompounds;
        _trainings = trainings;
        _constructions = constructions;
        _remainingMatter = remainingMatter;
        _playerStates = playerStates;
    }

    public bool IsValid(string playerId, Dictionary<Hexagon, Command> commands)
    {
        int requiredMatter = commands.Values.Sum(command => command.Training * _gameSettings.ArmyCost + command.Construction * _gameSettings.CompoundCost);
        if (requiredMatter > _playerStates[playerId].Matter)
        {
            return false;
        }
        int requiredSpace = commands.Values.Sum(command => command.Training * _gameSettings.RequiredSpace);
        if (requiredSpace + _playerStates[playerId].UsedSpace > _playerStates[playerId].AvailableSpace)
        {
            return false;
        }

        foreach ((Hexagon hexagon, Command command) in commands)
        {
            int newConstructionSides = command.Construction.Sum();
            int existingConstructionSides = _constructions
                .Where(x => x.Key >= _turn)
                .Sum(x => x.Value[playerId][hexagon].Sum());
            if (newConstructionSides + existingConstructionSides > _playerArmies[playerId][hexagon].Dot)
            {
                return false;
            }

            Army existingTrainings = _trainings
                .Where(x => x.Key >= _turn)
                .Select(x => x.Value[playerId][hexagon])
                .Sum();
        }

        return true;
    }

    public GameState NextTurn(PlayerHexagonCommands commands)
    {
        // ToDo: Compute required and available supply
        // Income
        var remainingMatter = _remainingMatter.ToBuilder();
        var playerStates = _playerStates.ToBuilder();
        foreach (var playerId in playerStates.Keys)
        {
            int income = 0;
            foreach ((Hexagon hexagon, Army army) in _playerArmies[playerId])
            {
                if (!_playerCompounds[playerId].TryGetValue(hexagon, out var compound) || compound.Plane == 0)
                {
                    continue;
                }
                Compound upcomingConstructions = _constructions.SumConstructions(_turn, playerId, hexagon);
                var idleArmy = army with
                {
                    Dot = (short)Math.Max(0, army.Dot - upcomingConstructions.Sum())
                };
                idleArmy = Army.Min(idleArmy, _gameSettings.MaximumNumberOfResourceGatheringUnits);
                int hexagonIncome = _gameSettings.Income * idleArmy;
                hexagonIncome = Math.Min(hexagonIncome, remainingMatter[hexagon]);
                remainingMatter[hexagon] -= hexagonIncome;
                income += hexagonIncome;
            }
            int spendMatter = 0;
            if (commands.TryGetValue(playerId, out var playerCommands))
            {
                spendMatter = playerCommands.Sum(x => x.Value.Construction * _gameSettings.CompoundCost +
                    x.Value.Training * _gameSettings.ArmyCost);
            }

            playerStates[playerId] = playerStates[playerId] with
            {
                Matter = playerStates[playerId].Matter + income - spendMatter,
            };
        }

        // Trainigs
        Dictionary<ushort, PlayerHexagonArmies> newTrainings = GetNewTrainings(commands);
        PlayerHexagonArmies playerArmies = _playerArmies.Copy();
        if (newTrainings.TryGetValue(_turn, out var trainings))
        {
            playerArmies.MergeArmies(trainings);
            foreach (var playerId in trainings.Keys)
            {
                int usedSpace = playerStates[playerId].UsedSpace + trainings[playerId].Values.Sum(army => army * _gameSettings.RequiredSpace);
                playerStates[playerId] = playerStates[playerId] with
                {
                    UsedSpace = usedSpace,
                };
            }
        }

        // Constructions
        Dictionary<ushort, PlayerHexagonCompound> newConstructions = GetNewConstructions(commands);
        var playerCompounds = _playerCompounds.Copy();
        if (newConstructions.TryGetValue(_turn, out var constructions))
        {
            playerCompounds.MergeCompounds(constructions);
            foreach (var playerId in constructions.Keys)
            {
                int availableSpace = playerStates[playerId].AvailableSpace + constructions[playerId].Values.Sum(construction => construction * _gameSettings.ProvidedSpace);
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
                    UsedSpace = playerStates[playerId].UsedSpace - losses[playerId] * _gameSettings.RequiredSpace
                };
            }
        }

        // Destroy Buildings
        foreach (var hexagon in _hexagons)
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
                        double damage = _gameSettings.StructureDamage * army;
                        var buildings = _buildings.OrderBy(x => _gameSettings.Armor[x]);
                        var destroyedCompound = Compound.Empty;
                        foreach (var building in buildings)
                        {
                            var destroyedBuildings = (short)Math.Min(compound[building], Math.Floor(damage / (double)_gameSettings.Armor[building]));
                            if (destroyedBuildings <= 0)
                            {
                                continue;
                            }
                            damage -= destroyedBuildings * _gameSettings.Armor[building];
                            destroyedCompound = destroyedCompound + Compound.FromBuilding(building, destroyedBuildings);
                        }
                        if (destroyedCompound.IsEmpty)
                        {
                            continue;
                        }
                        playerCompounds[compoundPlayerId].Merge(hexagon, -destroyedCompound);
                        playerStates[compoundPlayerId] = playerStates[compoundPlayerId] with
                        {
                            AvailableSpace = playerStates[compoundPlayerId].AvailableSpace - destroyedCompound * _gameSettings.ProvidedSpace
                        };
                    }
                }
            }
        }

        if (_turn == ushort.MaxValue)
        {
            throw new InvalidOperationException("Max turn limit reached.");
        }
        return new GameState(_gameSettings,
            (ushort)(1 + _turn),
            playerStates.ToImmutableDictionary(),
            playerArmies.ToImmutable(),
            playerCompounds.ToImmutable(),
            newTrainings.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutable()),
            newConstructions.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutable()),
            remainingMatter.ToImmutableDictionary());
    }

    private (Army lossesPlayer0, Army lossesPlayer1) Fight(string player1Id, Army army1, string player2Id, Army army2)
    {
        var army1Strength = army1.GetStrengthOver(army2, _gameSettings.FightExponent);
        var army2Strength = army2.GetStrengthOver(army1, _gameSettings.FightExponent);
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
                    ushort completionTurn = (ushort)(_turn + _gameSettings.TrainingDuration[unit]);
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
        foreach (var turn in _trainings.Keys)
        {
            if (!newTrainings.TryGetValue(turn, out var turnDict))
            {
                turnDict = [];
                newTrainings.Add(turn, turnDict);
            }
            foreach (var playerId in _trainings[turn].Keys)
            {
                if (!turnDict.TryGetValue(playerId, out var playerDict))
                {
                    playerDict = [];
                    turnDict.Add(playerId, playerDict);
                }
                foreach (var hexagon in _trainings[turn][playerId].Keys)
                {
                    playerDict.Merge(hexagon, _trainings[turn][playerId][hexagon]);
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
                    ushort completionTurn = (ushort)(_turn + _gameSettings.ConstructionDuration[building]);
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
        foreach (var turn in _constructions.Keys)
        {
            if (!newConstructions.TryGetValue(turn, out var turnDict))
            {
                turnDict = [];
                newConstructions.Add(turn, turnDict);
            }
            foreach (var playerId in _constructions[turn].Keys)
            {
                if (!turnDict.TryGetValue(playerId, out var playerDict))
                {
                    playerDict = [];
                    turnDict.Add(playerId, playerDict);
                }
                foreach (var hexagon in _constructions[turn][playerId].Keys)
                {
                    playerDict.Merge(hexagon, _constructions[turn][playerId][hexagon]);
                }
            }
        }

        return newConstructions;
    }
}