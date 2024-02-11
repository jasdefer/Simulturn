using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.Helper;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Ai;
public class RandomAi : IAi
{
    private readonly Random _random;

    public RandomAi(Random random)
    {
        _random = random;
    }

    public Order GetOrder(PlayerState playerState,
        GameSettings gameSettings,
        TurnMap<HexMap<PlayerMap<Fight>>> fightTurnPlayerMap,
        TurnMap<HexMap<PlayerMap<Army>>> armyTurnHexPlayerMap,
        TurnMap<HexMap<PlayerMap<Structure>>> structureTurnHexPlayerMap)
    {
        var availableSpace = ValidationHelper.GetAvailableSpace(gameSettings.StructureSettings.ProvidedSpace, playerState.StructureMap);
        var requiredSpace = ValidationHelper.GetRequiredSpace(gameSettings.ArmySettings.RequiredSpace, playerState.ArmyMap, playerState.TrainingMap, HexMap<Army>.Empty());
        if (availableSpace - requiredSpace < 5)
        {
            var getConstructions = OrderSpaceBringingConstructions(playerState.Matter, playerState.ConstructionMap, playerState.ArmyMap, gameSettings.StructureSettings.ProvidedSpace, gameSettings.StructureSettings.Cost, gameSettings.StructureSettings.ConstructionDuration);
        }
        throw new NotImplementedException();
    }

    private HexMap<Structure> OrderSpaceBringingConstructions(short matter,
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
            return HexMap<Structure>.Empty();
        }
        var constructionCount = Math.Min(1, (int)(0.2 * matter / cost[building]));
        constructionCount -= constructionMap.Values.Sum(x => x.Values.Select(y => y[building]).Sum());
        if(constructionCount < 1)
        {
            return HexMap<Structure>.Empty();
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
        return new HexMap<Structure>(constructions);
    }
}
