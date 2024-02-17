using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.Helper;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;
using System.Linq;

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
        Dictionary<Coordinates, Army> trainings = GetTrainings(playerState, gameSettings);
        Move[] moves = GetMoves(gameSettings, playerState.ArmyMap);

        return new Order(new HexMap<Army>(trainings), new HexMap<Structure>(constructions), moves.ToImmutableHashSet());
    }

    private Move[] GetMoves(GameSettings gameSettings, HexMap<Army> armyMap)
    {
        Move[] moves = [];
        return moves;
    }

    private Dictionary<Coordinates, Army> GetTrainings(PlayerState playerState, GameSettings gameSettings)
    {
        Dictionary<Coordinates, Army> armies = [];
        return armies;
    }

    private Dictionary<Coordinates, Structure> GetConstructions(GameSettings gameSettings, PlayerState playerState)
    {
        Dictionary<Coordinates, Structure> constructions = [];
        var availableSpace = ValidationHelper.GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap);
        var requiredSpace = ValidationHelper.GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, playerState.TrainingMap, HexMap<Army>.Empty());
        var remainingMatter = playerState.Matter;
        if (availableSpace - requiredSpace < 5)
        {
            var spaceBringingConstructions = OrderSpaceBringingConstructions(remainingMatter, playerState.ConstructionMap, playerState.ArmyMap, gameSettings.StructureSettings.ProvidedSpace, gameSettings.StructureSettings.Cost, gameSettings.StructureSettings.ConstructionDuration);
            constructions.Add(spaceBringingConstructions);
            remainingMatter -= (CollectionHelper.Sum(spaceBringingConstructions.Values) * gameSettings.StructureSettings.Cost).Sum();
        }
        var trainingBuildingConstructions = GetTrainingBuildingConstructions(remainingMatter, gameSettings, playerState.StructureMap);

        return constructions;
    }

    private object GetTrainingBuildingConstructions(short remainingMatter, GameSettings gameSettings, HexMap<Structure> structureMap)
    {
        foreach (var kvp in structureMap.Where(x => x.Value.Root > 0))
        {
            var target = new Structure(0, 0, 3, 3, 3, 0);
            target -= kvp.Value;
            while (target.Any())
            {

            }
        }
    }

    private Dictionary<Coordinates, Structure> OrderSpaceBringingConstructions(short matter,
                                                              TurnMap<HexMap<Structure>> constructionMap,
                                                              HexMap<Army> armyMap,
                                                              Structure providedSpace,
                                                              Structure cost,
                                                              Structure constructionDuration)
    {
        Building[] buildings = Enum.GetValues<Building>();
        var building = buildings.MaxBy(x => providedSpace[x] / (double)cost[x]);
        if(matter < cost[building])
        {
            return [];
        }
        var constructionCount = Math.Min(1, (int)(0.2 * matter / cost[building]));
        constructionCount -= constructionMap.Values.Sum(x => x.Values.Select(y => y[building]).Sum());
        if(constructionCount < 1)
        {
            return [];
        }
        Dictionary<Coordinates, Structure> constructions = [];
        foreach (var armyMapItem in armyMap.OrderByDescending(x => x.Value.Point))
        {
            var coordinates = armyMapItem.Key;
            var count = armyMapItem.Value.Point - constructionMap.Sum(x => x.Value.ContainsKey(coordinates) ? x.Value[coordinates].Sum() : 0);
            count = Math.Min(count, constructionCount);
            Dictionary<Building, short> dict = new Dictionary<Building, short>() { { building, Convert.ToInt16(count) } };
            Structure construction = Structure.FromBuildings(dict);
            constructions.Add(coordinates, construction);
            constructionCount -= count;
        }
        return constructions;
    }
}
