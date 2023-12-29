using SimulturnDomain.DataStructures;
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
                AddTrainings(trainings[player], order.Trainings, game.GameSettings.ArmySettings.TrainingDuration);
                AddTrainings(constructions[player], order.Constructions, game.GameSettings.StructureSettings.ConstructionDuration);
                MoveArmies(armies[player], order.Moves);
                CompleteTrainings(armies[player], trainings[player][turn], turn);
                CompleteConstructions(structures[player], constructions[player][turn], turn);
                short income = ComputeIncome(armies[player], game.GameSettings.ArmySettings.Income, matterPerHexagon);
                short trainingCost = GetTrainingCost(game.GameSettings.ArmySettings.Cost, order.Trainings);
                short constructionCost = GetConstructionCost(game.GameSettings.StructureSettings.Cost, order.Constructions);
                matters[player] += income - trainingCost - constructionCost;
            }
            ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> fights = GetFights(armies);
            result[turn] = BuildStates(matters, armies, structures, trainings, constructions, fights, matterPerHexagon);
        }
        return new TurnMap<State>(result);
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