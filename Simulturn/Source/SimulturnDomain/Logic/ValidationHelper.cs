using SimulturnDomain.DataStructures;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Logic;
public static class ValidationHelper
{
    public static bool IsValid(ushort turn, GameSettings gameSettings, PlayerState playerState, Order order)
    {
        ushort requiredMatter = GetRequiredMatter(order.Trainings,
                                                  order.Constructions,
                                                  gameSettings.ArmySettings.Cost,
                                                  gameSettings.StructureSettings.Cost);
        if (requiredMatter > playerState.Matter)
        {
            return false;
        }
        ushort requiredSpace = GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, order.Trainings);
        ushort availableSpace = GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap);
        if (requiredSpace > availableSpace)
        {
            return false;
        }
        foreach (var coordinates in order.Trainings.Keys)
        {
            Structure requiredStructures = GetRequiredStructures(turn, order.Trainings, playerState.TrainingMap);
            if (requiredStructures.AnyBuildingIsGreaterThan(playerState.StructureMap[coordinates]))
            {
                return false;
            }
        }
    }

    private static ushort GetAvailableSpace(Structure providedSpace, HexMap<Structure> structureMap)
    {
        throw new NotImplementedException();
    }

    private static ushort GetRequiredSpace(Army requiredSpace, HexMap<Army> armyMap, HexMap<Army> trainings)
    {
        throw new NotImplementedException();
    }

    private static ushort GetRequiredMatter(HexMap<Army> trainings, HexMap<Structure> constructions, Army cost1, Structure cost2)
    {
        throw new NotImplementedException();
    }
}
