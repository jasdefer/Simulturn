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
            result[turn] = BuildStates(matters, armies, structures, trainings, constructions, fights, matterPerHexagon);
        }
        return new TurnMap<State>(result);
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