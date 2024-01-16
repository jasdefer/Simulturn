using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
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
            Structure requiredStructures = GetRequiredStructures(turn, gameSettings.UnitTrainableBuilding,coordinates, order.Trainings[coordinates], playerState.TrainingMap);
            if (requiredStructures.AnyBuildingIsGreaterThan(playerState.StructureMap[coordinates]))
            {
                return false;
            }
        }

        foreach(var group in order.Moves.GroupBy(x => x.Origin))
        {
            var movingArmies = new Army();
            foreach (var army in group.Select(x => x.Army))
            {
                movingArmies += army;
            }
            var areValid = !playerState.ArmyMap.ContainsKey(group.Key)
                ? false
                : playerState.ArmyMap[group.Key].EachUnitIsGreaterOrEqualThan(movingArmies);
            if (!areValid)
            {
                return false;
            }
        }
        return true;
    }

    private static Structure GetRequiredStructures(ushort currentTurn, IDictionary<Unit, Building> unitTrainableBuilding, Coordinates coordinates, Army newTraining, TurnMap<HexMap<Army>> trainingMap)
    {
        var trainings = newTraining;
        foreach (var turn in trainingMap.Keys.Where(x => x>= currentTurn))
        {
            if (trainingMap[turn].ContainsKey(coordinates))
            {
                trainings += trainingMap[turn][coordinates];
            }
        }
        Structure structure = GetRequiredStructures(unitTrainableBuilding, trainings);
        return structure;

    }

    private static Structure GetRequiredStructures(IDictionary<Unit, Building> unitTrainableBuilding, Army trainings)
    {
        Unit[] units = Enum.GetValues<Unit>();
        Dictionary<Building, short> buildings = [];
        foreach (var unit in units)
        {
            buildings[unitTrainableBuilding[unit]] = trainings[unit];
        }
        return Structure.FromBuildings(buildings);
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
