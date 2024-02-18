using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Ai;
public class RandomAi : IAi
{
    private readonly Random _random;

    public RandomAi(Random random)
    {
        _random = random;
    }

    public Order GetOrder(PlayerState playerState,
        GameSettings gameSettings)
    {
        Dictionary<Coordinates, Structure> constructions = GetConstructions(gameSettings, playerState);
        short remainingMatter = Convert.ToInt16(playerState.Matter - constructions.Sum(x => (x.Value * gameSettings.StructureSettings.Cost).Sum()));
        Dictionary<Coordinates, Army> trainings = GetTrainings(remainingMatter, playerState, gameSettings);
        List<Move> moves = GetMoves(gameSettings, playerState.ArmyMap);

        return new Order(new HexMap<Army>(trainings), new HexMap<Structure>(constructions), moves.ToImmutableHashSet());
    }

    private List<Move> GetMoves(GameSettings gameSettings, HexMap<Army> armyMap)
    {
        List<Move> moves = [];
        foreach (var kvp in armyMap.Where(x => x.Value.Sum() - x.Value.Point > 3))
        {
            var destination = HexDirections.All
                    .OrderBy(x => _random.NextDouble())
                    .Where(x => gameSettings.Coordinates.Contains(kvp.Key.GetNeighbor(x)))
                    .First();
            Move move = new(kvp.Key, destination, kvp.Value - new Army(point: kvp.Value.Point));
            moves.Add(move);
        }
        return moves;
    }

    private Dictionary<Coordinates, Army> GetTrainings(short remainingMatter, PlayerState playerState, GameSettings gameSettings)
    {
        var remainingSpace = ValidationHelper.GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap)
            - ValidationHelper.GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, playerState.TrainingMap, HexMap<Army>.Empty());

        Dictionary<Coordinates, Army> armies = [];
        foreach (var coordinates in playerState.StructureMap.
            Where(x => x.Value.Root > 0).Select(x => x.Key))
        {
            if (remainingMatter <= 0)
            {
                break;
            }
            var current = playerState.ArmyMap.ToDictionary().GetOrDefault(coordinates);
            current += CollectionHelper.Sum(playerState.TrainingMap.Select(x => x.Value.ToDictionary().GetOrDefault(coordinates)));

            if (current.AnyUnitIsSmallerThan(gameSettings.HexagonSettingsPerCoordinates[coordinates].MaxNumberOfUnitsGeneratingIncome))
            {
                Dictionary<Unit, short> newUnits = [];
                var target = gameSettings.HexagonSettingsPerCoordinates[coordinates].MaxNumberOfUnitsGeneratingIncome - current;
                foreach (var unit in Units.All.Where(x => target[x] > 0))
                {
                    var count = Math.Min(target[unit], gameSettings.ArmySettings.Cost[unit] / remainingMatter);
                    var productionCapability = playerState.StructureMap[coordinates][gameSettings.UnitTrainableBuilding[unit]];
                    productionCapability -= Convert.ToInt16(playerState.TrainingMap.Sum(x => x.Value[coordinates][unit]));
                    count = Math.Min(count, productionCapability);
                    count = Math.Min(count, gameSettings.ArmySettings.RequiredSpace[unit] / remainingSpace);
                    if (count > 0)
                    {
                        newUnits[unit] = Convert.ToInt16(count);
                        remainingMatter -= Convert.ToInt16(gameSettings.ArmySettings.Cost[unit] * count);
                        remainingSpace -= count * gameSettings.ArmySettings.RequiredSpace[unit];
                    }
                }
                if (newUnits.Any())
                {
                    armies.Add(coordinates, Army.FromUnits(newUnits));
                }
            }
        }

        Unit[] units = new[] { Unit.Circle, Unit.Square, Unit.Triangle }.OrderBy(x => _random.NextDouble()).ToArray();
        foreach (var kvp in playerState.StructureMap
            .Where(x => x.Value.Pyramid > 0 || x.Value.Sphere > 0 || x.Value.Cube > 0))
        {
            Dictionary<Unit, short> newUnits = [];
            if (remainingMatter <= 0)
            {
                break;
            }
            foreach (var unit in units)
            {
                var count = remainingMatter / gameSettings.ArmySettings.Cost[unit];
                count = Math.Min(count, kvp.Value[gameSettings.UnitTrainableBuilding[unit]]);
                count -= playerState.TrainingMap.Where(x => x.Value.ContainsKey(kvp.Key)).Sum(x => x.Value[kvp.Key][unit]);
                count = Math.Min(count, remainingSpace / gameSettings.ArmySettings.RequiredSpace[unit]);
                if (count > 0)
                {
                    newUnits.Add(unit, Convert.ToInt16(count));
                    remainingMatter -= Convert.ToInt16(count * gameSettings.ArmySettings.Cost[unit]);
                    remainingSpace -= count * gameSettings.ArmySettings.RequiredSpace[unit];
                }
            }
            if (newUnits.Any())
            {
                armies.Add(kvp.Key, Army.FromUnits(newUnits));
            }
        }
        return armies;
    }

    private Dictionary<Coordinates, Structure> GetConstructions(GameSettings gameSettings, PlayerState playerState)
    {
        Dictionary<Coordinates, short> remainigPoints = playerState.ArmyMap.Where(x => x.Value.Point > 0).ToDictionary(x => x.Key, x => x.Value.Point);
        foreach (var constructionMap in playerState.ConstructionMap)
        {
            foreach (var kvp in constructionMap.Value.Where(x => remainigPoints.ContainsKey(x.Key)))
            {
                remainigPoints[kvp.Key] -= kvp.Value.Sum();
            }
        }
        Dictionary<Coordinates, Structure> constructions = [];
        var availableSpace = ValidationHelper.GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap);
        var requiredSpace = ValidationHelper.GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, playerState.TrainingMap, HexMap<Army>.Empty());
        var remainingMatter = playerState.Matter;
        if (availableSpace - requiredSpace < gameSettings.StructureSettings.ProvidedSpace.Axis)
        {
            var spaceBringingConstructions = OrderSpaceBringingConstructions(remainingMatter, remainigPoints, playerState.ConstructionMap, playerState.ArmyMap, gameSettings.StructureSettings.ProvidedSpace, gameSettings.StructureSettings.Cost, gameSettings.StructureSettings.ConstructionDuration);
            constructions.Add(spaceBringingConstructions);
            remainingMatter -= (CollectionHelper.Sum(spaceBringingConstructions.Values) * gameSettings.StructureSettings.Cost).Sum();
        }
        var trainingBuildingConstructions = GetTrainingBuildingConstructions(remainingMatter, remainigPoints, gameSettings, playerState.StructureMap, playerState.ConstructionMap);
        constructions.Add(trainingBuildingConstructions);
        remainingMatter -= (CollectionHelper.Sum(trainingBuildingConstructions.Values) * gameSettings.StructureSettings.Cost).Sum();

        var expansions = GetExpansionConstructions(remainingMatter, remainigPoints, gameSettings, playerState);
        constructions.Add(expansions);
        remainingMatter -= (CollectionHelper.Sum(expansions.Values) * gameSettings.StructureSettings.Cost).Sum();

        return constructions;
    }

    private Dictionary<Coordinates, Structure> GetExpansionConstructions(short remainingMatter, Dictionary<Coordinates, short> remainigPoints, GameSettings gameSettings, PlayerState playerState)
    {
        Dictionary<Coordinates, Structure> result = [];
        foreach (var coordinates in playerState.ArmyMap.Where(x => x.Value.Point > 0).Select(x => x.Key))
        {
            if (playerState.StructureMap.ContainsKey(coordinates) &&
                playerState.StructureMap[coordinates].Root == 0 &&
                playerState.ConstructionMap.All(x => x.Value.ContainsKey(coordinates) && x.Value[coordinates].Root == 0) &&
                remainingMatter >= gameSettings.StructureSettings.Cost.Root &&
                remainigPoints.ContainsKey(coordinates) &&
                remainigPoints[coordinates] > 0)
            {
                result.Add(coordinates, new Structure(1));
                remainigPoints[coordinates] -= 1;
            }
        }
        return result;
    }

    private Dictionary<Coordinates, Structure> GetTrainingBuildingConstructions(short remainingMatter, Dictionary<Coordinates, short> remainingPoints, GameSettings gameSettings, HexMap<Structure> structureMap, TurnMap<HexMap<Structure>> constructions)
    {
        Building[] buildings = [Building.Cube, Building.Pyramid, Building.Sphere];
        Dictionary<Coordinates, Structure> newConstructions = [];
        foreach (var kvp in structureMap.Where(x => x.Value.Root > 0))
        {
            var target = new Structure(0, 3, 3, 3, 0, 0);
            target -= kvp.Value;
            target -= CollectionHelper.Sum(constructions.Select(x => x.Value[kvp.Key]));
            buildings = buildings.OrderBy(x => _random.NextDouble()).ToArray();
            Dictionary<Building, short> result = [];
            foreach (var building in buildings)
            {
                var count = remainingMatter / gameSettings.StructureSettings.Cost[building];
                count = Math.Min(count, target[building]);
                if (remainingPoints.ContainsKey(kvp.Key))
                {
                    count = Math.Min(count, remainingPoints[kvp.Key]);
                }
                if (count > 0)
                {
                    result.Add(building, Convert.ToInt16(count));
                    remainingMatter -= Convert.ToInt16(count * gameSettings.StructureSettings.Cost[building]);
                    remainingPoints[kvp.Key] -= Convert.ToInt16(count);
                }
            }
            newConstructions.Add(kvp.Key, Structure.FromBuildings(result));
        }
        return newConstructions;
    }

    private Dictionary<Coordinates, Structure> OrderSpaceBringingConstructions(short matter,
                                                              Dictionary<Coordinates, short> remainigPoints,
                                                              TurnMap<HexMap<Structure>> constructionMap,
                                                              HexMap<Army> armyMap,
                                                              Structure providedSpace,
                                                              Structure cost,
                                                              Structure constructionDuration)
    {
        Building[] buildings = Enum.GetValues<Building>();
        var building = buildings.MaxBy(x => providedSpace[x] / (double)cost[x]);
        if (matter < cost[building])
        {
            return [];
        }
        var constructionCount = Math.Min(1, (int)(0.2 * matter / cost[building]));
        constructionCount -= constructionMap.Values.Sum(x => x.Values.Select(y => y[building]).Sum());
        if (constructionCount < 1)
        {
            return [];
        }
        Dictionary<Coordinates, Structure> constructions = [];
        foreach (var coordinates in remainigPoints.OrderByDescending(x => x.Value).Select(x => x.Key).ToArray())
        {
            var count = Math.Min(remainigPoints[coordinates], constructionCount);
            if (count > 0)
            {
                Dictionary<Building, short> dict = new Dictionary<Building, short>() { { building, Convert.ToInt16(count) } };
                Structure construction = Structure.FromBuildings(dict);
                constructions.Add(coordinates, construction);
                constructionCount -= count;
                remainigPoints[coordinates] -= Convert.ToInt16(count);
            }
        }
        return constructions;
    }
}
