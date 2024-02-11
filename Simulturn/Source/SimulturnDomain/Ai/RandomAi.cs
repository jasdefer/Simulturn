using SimulturnDomain.DataStructures;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
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
        var budget = Math.Max(matter, cost)
    }
}
