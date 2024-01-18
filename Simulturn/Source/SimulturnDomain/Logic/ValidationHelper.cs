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
        // Validate matter
        ushort requiredMatter = GetRequiredMatter(order.Trainings,
                                                  order.Constructions,
                                                  gameSettings.ArmySettings.Cost,
                                                  gameSettings.StructureSettings.Cost);
        if (requiredMatter > playerState.Matter)
        {
            return false;
        }

        // Validate Space
        ushort requiredSpace = GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, order.Trainings);
        ushort availableSpace = GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap);
        if (requiredSpace > availableSpace)
        {
            return false;
        }

        // Validate structure for trainings
        foreach (var coordinates in order.Trainings.Keys)
        {
            Structure requiredStructures = GetRequiredStructures(turn,
                                                                 gameSettings.UnitTrainableBuilding,
                                                                 coordinates,
                                                                 order.Trainings[coordinates],
                                                                 playerState.TrainingMap);
            if (requiredStructures.AnyBuildingIsGreaterThan(playerState.StructureMap[coordinates]))
            {
                return false;
            }
        }

        // Validate points for constructions
        foreach (var coordinates in order.Constructions.Keys)
        {
            var constructionCount = GetConstructionCount(turn,
                                                         coordinates,
                                                         order.Constructions[coordinates],
                                                         playerState.ConstructionMap);
            if(constructionCount> playerState.ArmyMap[coordinates].Point)
            {
                return false;
            }
        }

        // Validate movements
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

    public static short GetConstructionCount(ushort currentTurn, Coordinates coordinates, Structure order, TurnMap<HexMap<Structure>> constructionMap)
    {
        var constructionCount = order.Sum();
        foreach (var turn in constructionMap.Keys.Where(x => x >= currentTurn))
        {
            constructionCount += constructionMap[turn][coordinates].Sum();
        }
        return constructionCount;
    }

    public static Structure GetRequiredStructures(ushort currentTurn, IDictionary<Unit, Building> unitTrainableBuilding, Coordinates coordinates, Army newTraining, TurnMap<HexMap<Army>> trainingMap)
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

    public static Structure GetRequiredStructures(IDictionary<Unit, Building> unitTrainableBuilding, Army trainings)
    {
        Unit[] units = Enum.GetValues<Unit>();
        Dictionary<Building, short> buildings = [];
        foreach (var unit in units)
        {
            buildings[unitTrainableBuilding[unit]] = trainings[unit];
        }
        return Structure.FromBuildings(buildings);
    }

    public static ushort GetAvailableSpace(Structure providedSpace, HexMap<Structure> structureMap)
    {
        ushort space = 0;
        foreach (var structure in structureMap.Values)
        {
            space += Convert.ToUInt16((structure * providedSpace).Sum());
        }
        return space;
    }

    public static ushort GetRequiredSpace(Army requiredSpace, HexMap<Army> armyMap, HexMap<Army> trainings)
    {
        ushort space = 0;
        foreach (var army in armyMap.Values)
        {
            space += Convert.ToUInt16((requiredSpace * army).Sum());
        }
        foreach (var army in trainings.Values)
        {
            space += Convert.ToUInt16((requiredSpace * army).Sum());
        }
        return space;
    }

    public static ushort GetRequiredMatter(HexMap<Army> trainings, HexMap<Structure> constructions, Army armyCost, Structure structureCost)
    {
        ushort requiredMatter = 0;
        foreach (var army in trainings.Values)
        {
            requiredMatter += Convert.ToUInt16((army * armyCost).Sum());
        }
        foreach (var structure in constructions.Values)
        {
            requiredMatter += Convert.ToUInt16((structure * structureCost).Sum());
        }
        return requiredMatter;
    }
}
