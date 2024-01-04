using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class StateHelper
{
    /// <summary>
    /// Compute the current state of the game based on all orders by each player.
    /// </summary>
    public static TurnMap<State> GetStatesPerPlayer(Game game)
    {
        ushort turns = game.TurnDictionary.NumberOfTurns;
        var armyMap = game.GameSettings.Coordinates.ToDictionary(x => x, x => new Army());
        var structureMap = game.GameSettings.Coordinates.ToDictionary(x => x, x => new Structure());

        Dictionary<string, short> matters = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => game.GameSettings.StartMatter);
        Dictionary<string, Dictionary<Coordinates, Army>> armies = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => armyMap.ToDictionary());
        Dictionary<string, Dictionary<Coordinates, Structure>> structures = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => structureMap.ToDictionary());
        Dictionary<string, Dictionary<ushort, Dictionary<Coordinates, Army>>> trainings = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => new Dictionary<ushort, Dictionary<Coordinates, Army>>());
        Dictionary<string, Dictionary<ushort, Dictionary<Coordinates, Structure>>> constructions = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => new Dictionary<ushort, Dictionary<Coordinates, Structure>>());
        Dictionary<Coordinates, ushort> matterPerHexagon = game.GameSettings.HexagonSettingsPerCoordinates.Keys.ToDictionary(x => x, x => game.GameSettings.HexagonSettingsPerCoordinates[x].Matter);

        SetupStartArmiesAndStructures(game.GameSettings.HexagonSettingsPerCoordinates,
                                      game.GameSettings.ArmySettings.StartUnits,
                                      game.GameSettings.StructureSettings.StartStructures,
                                      armies,
                                      structures);

        Dictionary<ushort, State> result = new Dictionary<ushort, State>();
        for (ushort turn = 0; turn < turns; turn++)
        {
            foreach (var player in game.TurnDictionary.PlayerIds)
            {
                Order order = game.TurnDictionary.GetOrder(player, turn);
                short income = GetIncome(armies[player], game.GameSettings.ArmySettings.Income, matterPerHexagon);
                short trainingCost = GetTrainingCost(game.GameSettings.ArmySettings.Cost, order.Trainings);
                short constructionCost = GetConstructionCost(game.GameSettings.StructureSettings.Cost, order.Constructions);
                matters[player] += Convert.ToInt16(income - trainingCost - constructionCost);
                AddTrainings(turn, trainings[player], order.Trainings, game.GameSettings.ArmySettings.TrainingDuration);
                AddConstructions(turn, constructions[player], order.Constructions, game.GameSettings.StructureSettings.ConstructionDuration);
                MoveArmies(armies[player], order.Moves);
                CompleteTrainings(armies[player], trainings[player][turn]);
                CompleteConstructions(structures[player], constructions[player][turn]);
            }
            ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> fights = GetFights(armies);
            KillUnitsAndDestroyBuildings(fights, armies, structures, game.GameSettings.FightExponent, game.GameSettings.ArmySettings.StructureDamage, game.GameSettings.StructureSettings.Armor);
            result[turn] = BuildStates(matters, armies, structures, trainings, constructions, fights, matterPerHexagon);
        }
        return new TurnMap<State>(result);
    }

    private static (Army arm1Losses, Army arm2Losses) Fight(Army army1, Army army2, double fightExponent)
    {
        var army1Strength = army1.GetStrengthOver(army2, fightExponent);
        var army2Strength = army2.GetStrengthOver(army1, fightExponent);
        Army arm1Losses;
        Army arm2Losses;
        if (army1Strength > army2Strength)
        {
            var fraction = army2Strength / army1Strength;
            arm1Losses = army1.MultiplyAndRoundUp(1 - fraction);
            arm2Losses = army2;
        }
        else
        {
            var fraction = army1Strength / army2Strength;
            arm2Losses = army2.MultiplyAndRoundUp(1 - fraction);
            arm1Losses = army1;
        }

        return (arm1Losses, arm2Losses);
    }

    public static Structure Destroy(Army army, Structure structure, Army damage, Structure armor)
    {
        var totalDamage = (army * damage).Sum();
        var buildings = Enum.GetValues<Building>().OrderBy(x => armor[x]).ToArray();
        Structure losses = new Structure();
        int index = 0;
        while (totalDamage > 0)
        {
            Building building = buildings[index];
            short count = Convert.ToInt16(Math.Floor(totalDamage / (double)structure[building]));
            if (count <= 0)
            {
                totalDamage = 0;
            }
            else
            {
                totalDamage -= Convert.ToInt16(count * structure[building]);
            }
            losses = losses.Add(building, count);
        }
        return losses;
    }

    private static void KillUnitsAndDestroyBuildings(ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> fights,
        Dictionary<string, Dictionary<Coordinates, Army>> armies,
        Dictionary<string, Dictionary<Coordinates, Structure>> structures,
        double fightExponent,
        Army damage,
        Structure armor)
    {
        foreach (var coordinates in fights.Keys)
        {
            var players = fights[coordinates].Keys.ToArray();
            var playerCount = players.Length;
            Army[] losses = new Army[playerCount];
            for (int i = 0; i < playerCount - 1; i++)
            {
                string player1 = players[i];
                var army1 = fights[coordinates][player1];
                for (int j = i + 1; j < playerCount; j++)
                {
                    string player2 = players[j];
                    if (fights[coordinates].ContainsKey(player2))
                    {
                        var army2 = fights[coordinates][player2];
                        (var army1Losses, var army2Losses) = Fight(army1, army2, fightExponent);
                        losses[i] += army1Losses;
                        losses[j] += army2Losses;
                    }
                }
                Army remainingArmy = armies[player1][coordinates] - losses[i];
                remainingArmy = remainingArmy.Max(Army.Empty);
                armies[players[i]][coordinates] = remainingArmy;
                if (losses[i] < army1)
                {
                    var losers = fights[coordinates].Keys.Where(x => x != player1 && structures[x][coordinates].Any());
                    foreach (var player in losers)
                    {
                        structures[player][coordinates] -= Destroy(remainingArmy, structures[player][coordinates], damage, armor);
                    }
                }
            }
        }
    }

    private static State BuildStates(Dictionary<string, short> matters,
                                     Dictionary<string, Dictionary<Coordinates, Army>> armies,
                                     Dictionary<string, Dictionary<Coordinates, Structure>> structures,
                                     Dictionary<string, Dictionary<ushort, Dictionary<Coordinates, Army>>> trainings,
                                     Dictionary<string, Dictionary<ushort, Dictionary<Coordinates, Structure>>> constructions,
                                     ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> fights,
                                     Dictionary<Coordinates, ushort> matterPerHexagon)
    {

        var playerStates = new Dictionary<string, PlayerState>();
        foreach (var player in matters.Keys)
        {
            PlayerState playerState = new PlayerState(matters[player],
                new HexMap<Army>(armies[player]),
                new HexMap<Structure>(structures[player]),
                new TurnMap<HexMap<Army>>(trainings[player].ToDictionary(x => x.Key, x => new HexMap<Army>(x.Value))),
                new TurnMap<HexMap<Structure>>(constructions[player].ToDictionary(x => x.Key, x => new HexMap<Structure>(x.Value))),
                new HexMap<ImmutableDictionary<string, Army>>(fights.Where(x => x.Value.ContainsKey(player)).ToDictionary()));
            playerStates[player] = playerState;
        }
        State state = new State(playerStates.ToImmutableDictionary(),
            new HexMap<ushort>(matterPerHexagon),
            new HexMap<ImmutableDictionary<string, Army>>(fights));
        return state;
    }

    private static ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> GetFights(Dictionary<string, Dictionary<Coordinates, Army>> armiesPerPlayer)
    {
        HashSet<Coordinates> coordinates = armiesPerPlayer.SelectMany(x => x.Value.Keys).ToHashSet();
        Dictionary<Coordinates, ImmutableDictionary<string, Army>> fights = new Dictionary<Coordinates, ImmutableDictionary<string, Army>>();
        foreach (var coordinae in coordinates)
        {
            KeyValuePair<string, Dictionary<Coordinates, Army>>[] fightingArmies = armiesPerPlayer
                .Where(x => x.Value.ContainsKey(coordinae) && x.Value[coordinae].Sum() > 0)
                .ToArray();
            if (fightingArmies.Length > 1)
            {
                var fight = fightingArmies.ToImmutableDictionary(x => x.Key, x => x.Value[coordinae]);
                fights.Add(coordinae, fight);
            }
        }
        return fights.ToImmutableDictionary();
    }

    private static void CompleteConstructions(Dictionary<Coordinates, Structure> structures, Dictionary<Coordinates, Structure> constructions)
    {
        foreach (var construction in constructions)
        {
            structures[construction.Key] += construction.Value;
        }
    }

    private static void CompleteTrainings(Dictionary<Coordinates, Army> armies, Dictionary<Coordinates, Army> trainings)
    {
        foreach (var training in trainings)
        {
            armies[training.Key] += training.Value;
        }
    }

    private static void MoveArmies(Dictionary<Coordinates, Army> armies, IImmutableSet<Move> moves)
    {
        foreach (var move in moves)
        {
            armies[move.Origin] -= move.Army;
            Coordinates destination = move.Origin.GetNeighbor(move.Direction);
            armies[destination] += move.Army;
        }
    }

    private static void AddConstructions(ushort turn,
        Dictionary<ushort, Dictionary<Coordinates, Structure>> constructions,
        ImmutableDictionary<Coordinates, Structure> orderConstructions,
        Structure constructionDuration)
    {
        var buildings = Enum.GetValues(typeof(Building));
        foreach (KeyValuePair<Coordinates, Structure> orderConstruction in orderConstructions)
        {
            foreach (Building building in buildings)
            {
                if (orderConstruction.Value[building] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(turn + constructionDuration[building]);
                    if (!constructions.ContainsKey(completionTurn))
                    {
                        constructions.Add(completionTurn, new Dictionary<Coordinates, Structure>());
                    }
                    if (!constructions[completionTurn].ContainsKey(orderConstruction.Key))
                    {
                        constructions[completionTurn].Add(orderConstruction.Key, new Structure());
                    }
                    Structure newStructure = constructions[completionTurn][orderConstruction.Key].Add(building, orderConstruction.Value[building]);
                    constructions[completionTurn][orderConstruction.Key] = newStructure;
                }
            }
        }
    }

    private static void AddTrainings(ushort turn, Dictionary<ushort, Dictionary<Coordinates, Army>> trainings,
        ImmutableDictionary<Coordinates, Army> orderTrainings,
        Army trainingDuration)
    {
        var units = Enum.GetValues(typeof(Unit));
        foreach (KeyValuePair<Coordinates, Army> orderTraining in orderTrainings)
        {
            foreach (Unit unit in units)
            {
                if (orderTraining.Value[unit] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(turn + trainingDuration[unit]);
                    if (!trainings.ContainsKey(completionTurn))
                    {
                        trainings.Add(completionTurn, new Dictionary<Coordinates, Army>());
                    }
                    if (!trainings[completionTurn].ContainsKey(orderTraining.Key))
                    {
                        trainings[completionTurn].Add(orderTraining.Key, new Army());
                    }
                    Army newArmy = trainings[completionTurn][orderTraining.Key].Add(unit, orderTraining.Value[unit]);
                    trainings[completionTurn][orderTraining.Key] = newArmy;
                }
            }
        }
    }

    private static short GetConstructionCost(Structure cost, ImmutableDictionary<Coordinates, Structure> constructions)
    {
        short constructionCosts = 0;
        foreach (var construction in constructions.Values)
        {
            constructionCosts += (construction * cost).Sum();
        }
        return constructionCosts;
    }

    private static short GetTrainingCost(Army cost, ImmutableDictionary<Coordinates, Army> trainings)
    {
        short trainingCost = 0;
        foreach (var training in trainings.Values)
        {
            trainingCost += (training * cost).Sum();
        }
        return trainingCost;
    }

    private static short GetIncome(Dictionary<Coordinates, Army> armies, Army incomeSettings, Dictionary<Coordinates, ushort> matterPerHexagon)
    {
        short income = 0;
        foreach (var coordinate in armies.Keys)
        {
            short incomeAtHex = (armies[coordinate] * incomeSettings).Sum();
            incomeAtHex = Math.Min(incomeAtHex, Convert.ToInt16(matterPerHexagon[coordinate]));
            matterPerHexagon[coordinate] -= Convert.ToUInt16(incomeAtHex);
            income += incomeAtHex;
        }
        return income;
    }

    private static void SetupStartArmiesAndStructures(HexMap<HexagonSettings> hexagonSettingsPerCoordinates,
        Army startAmy,
        Structure startStructure,
        Dictionary<string, Dictionary<Coordinates, Army>> armies,
        Dictionary<string, Dictionary<Coordinates, Structure>> structures)
    {
        int playerIndex = 0;
        string[] playerIds = armies.Keys.ToArray();
        foreach (var coordinates in hexagonSettingsPerCoordinates.Keys)
        {
            if (hexagonSettingsPerCoordinates[coordinates].IsStartHexagon)
            {
                string player = playerIds[playerIndex];
                armies[player][coordinates] = startAmy;
                structures[player][coordinates] = startStructure;
                playerIndex++;
                if (playerIndex >= playerIds.Length)
                {
                    throw new InvalidOperationException("Number of players exceeds number of starting hexagon.");
                }
            }
        }
    }
}