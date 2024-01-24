using SimulturnDomain.DataStructures;
using SimulturnDomain.Helper;
using SimulturnDomain.Model;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class VisibilityHelper
{
    public static (HexMap<PlayerMap<Army>> ArmyMap, HexMap<PlayerMap<Structure>> StructureMap) GetVisibilityMaps(State state, string currentPlayer)
    {

        ImmutableHashSet<Coordinates> visibleCoordinates = state.PlayerStates[currentPlayer].ArmyMap.Keys
            .Union(state.PlayerStates[currentPlayer].StructureMap.Keys)
            .Distinct()
            .SelectMany(x => x.GetNeighbors())
            .ToImmutableHashSet();
        Dictionary<Coordinates, Dictionary<string, Army>> armyMap = visibleCoordinates.ToDictionary(x => x, x => new Dictionary<string, Army>());
        Dictionary<Coordinates, Dictionary<string, Structure>> structureMap = visibleCoordinates.ToDictionary(x => x, x => new Dictionary<string, Structure>());
        foreach (var player in state.PlayerStates.Keys)
        {
            var visibleCoordinatesWithEnemyUnits = state.PlayerStates[player].ArmyMap.Keys
                .Where(visibleCoordinates.Contains);
            foreach (var coordinates in visibleCoordinatesWithEnemyUnits)
            {
                Army army = state.PlayerStates[player].ArmyMap[coordinates];
                armyMap[coordinates].Add(player, army);
            }
            var visibleCoordinatesWithEnemyStructures = state.PlayerStates[player].StructureMap.Keys
                .Where(visibleCoordinates.Contains);
            foreach (var coordinates in visibleCoordinatesWithEnemyStructures)
            {
                Structure structure = state.PlayerStates[player].StructureMap[coordinates];
                structureMap[coordinates].Add(player, structure);
            }
        }

        return (armyMap.ToHexPlayerMap(), structureMap.ToHexPlayerMap());
    }
}