using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class StateHelper
{
    public static State GetInitialState(GameSettings gameSettings, IEnumerable<string> players)
    {

        Coordinates[] startCoordinates = gameSettings.HexagonSettingsPerCoordinates.Keys
            .Where(x => gameSettings.HexagonSettingsPerCoordinates[x].IsStartHexagon)
            .ToArray();
        byte playerIndex = 0;
        var playerStates = new Dictionary<string, PlayerState>();
        foreach (var player in players)
        {
            short matter = gameSettings.StartMatter;
            Coordinates coordinates = startCoordinates[playerIndex++];
            HexMap<Army> armies = new(new Dictionary<Coordinates, Army>() { { coordinates, gameSettings.ArmySettings.StartUnits } });
            HexMap<Structure> structures = new(new Dictionary<Coordinates, Structure>() { { coordinates, gameSettings.StructureSettings.StartStructures } });
            PlayerState playerState = new PlayerState(matter, armies, structures, TurnMap<HexMap<Army>>.Empty(), TurnMap<HexMap<Structure>>.Empty());
            playerStates[player] = playerState;
        }

        HexMap<ushort> remainingMatter = new(gameSettings.HexagonSettingsPerCoordinates.Keys.ToDictionary(x => x, x => gameSettings.HexagonSettingsPerCoordinates[x].Matter));
        HexMap<PlayerMap<Fight>> fights = HexMap<PlayerMap<Fight>>.Empty();

        PlayerMap<PlayerState> stateMap = new(playerStates);
        State state = new(stateMap, remainingMatter, fights);
        return state;
    }

    public static State GetState(ushort turn, State state, IReadOnlyDictionary<string, Order> orders, GameSettings gameSettings)
    {
        Dictionary<string, PlayerState> playerStates = new();
        HexMap<ushort> remainingMatter = state.RemainingMatter;
        foreach (var player in orders.Keys)
        {
            Order order = orders[player];
            PlayerState playerState = state.PlayerStates[player];
            HexMap<ushort> revenue = GetRevenue(playerState.ArmyMap, gameSettings.ArmySettings.Income, state.RemainingMatter);
            ushort income = IncomeHelper.GetIncome(revenue, playerState.ArmyMap, gameSettings.ArmySettings.RequiredSpace, gameSettings.UpkeepLevels);
            remainingMatter = RemoveMatter(remainingMatter, revenue);
            short trainingCost = GetTrainingCost(gameSettings.ArmySettings.Cost, order.Trainings);
            short constructionCost = GetConstructionCost(gameSettings.StructureSettings.Cost, order.Constructions);
            short matter = Convert.ToInt16(playerState.Matter + income - trainingCost - constructionCost);
            TurnMap<HexMap<Army>> trainings = AddTrainings(turn, playerState.TrainingMap, order.Trainings, gameSettings.ArmySettings.TrainingDuration);
            TurnMap<HexMap<Structure>> constructions = AddConstructions(turn, playerState.ConstructionMap, order.Constructions, gameSettings.StructureSettings.ConstructionDuration);
            HexMap<Army> armyMap = MoveArmies(playerState.ArmyMap, order.Moves);
            if (trainings.ContainsKey(turn))
            {
                armyMap = CompleteTrainings(armyMap, trainings[turn]);
            }
            HexMap<Structure> structureMap = playerState.StructureMap;
            if (constructions.ContainsKey(turn))
            {
                structureMap = CompleteConstructions(structureMap, constructions[turn]);
            }
            PlayerState preFightState = new PlayerState(matter, armyMap, structureMap, trainings, constructions);
            playerStates[player] = preFightState;
        }

        HexMap<PlayerMap<Fight>> fights = GetFights(gameSettings.FightExponent, playerStates.ToDictionary(x => x.Key, x => x.Value.ArmyMap));
        foreach (var player in orders.Keys)
        {
            HexMap<Army> newArmies = GetArmies(playerStates[player].ArmyMap, fights);
            playerStates[player] = playerStates[player] with { ArmyMap = newArmies };
        }
        PlayerMap<PlayerState> playerStateMap = new(playerStates);
        State newState = new(playerStateMap, remainingMatter, fights);
        return newState;
    }

    private static HexMap<ushort> RemoveMatter(HexMap<ushort> remainingMatter, HexMap<ushort> revenue)
    {
        var result = remainingMatter.ToDictionary();
        foreach (var coordinates in revenue.Keys)
        {
            result[coordinates] -= revenue[coordinates];
        }
        return new HexMap<ushort>(result);
    }

    private static (Army arm1Losses, Army arm2Losses) Fight(Army army1, Army army2, double fightExponent)
    {
        var army1Strength = army1.GetStrengthOver(army2, fightExponent);
        var army2Strength = army2.GetStrengthOver(army1, fightExponent);
        Army arm1Losses;
        Army arm2Losses;
        if (army1Strength > army2Strength)
        {
            var fraction = army2Strength / army1Strength;
            arm1Losses = army1.MultiplyAndRoundUp(1 - fraction);
            arm2Losses = army2;
        }
        else
        {
            var fraction = army1Strength / army2Strength;
            arm2Losses = army2.MultiplyAndRoundUp(1 - fraction);
            arm1Losses = army1;
        }

        return (arm1Losses, arm2Losses);
    }

    public static Structure Destroy(Army army, Structure structure, Army damage, Structure armor)
    {
        var totalDamage = (army * damage).Sum();
        var buildings = Enum.GetValues<Building>().OrderBy(x => armor[x]).ToArray();
        Structure losses = new Structure();
        int index = 0;
        while (totalDamage > 0)
        {
            Building building = buildings[index];
            short count = Convert.ToInt16(Math.Floor(totalDamage / (double)structure[building]));
            if (count <= 0)
            {
                totalDamage = 0;
            }
            else
            {
                totalDamage -= Convert.ToInt16(count * structure[building]);
            }
            losses = losses.Add(building, count);
        }
        return losses;
    }

    private static void KillUnitsAndDestroyBuildings(ImmutableDictionary<Coordinates, ImmutableDictionary<string, Army>> fights,
        Dictionary<string, Dictionary<Coordinates, Army>> armies,
        Dictionary<string, Dictionary<Coordinates, Structure>> structures,
        double fightExponent,
        Army damage,
        Structure armor)
    {
        foreach (var coordinates in fights.Keys)
        {
            var players = fights[coordinates].Keys.ToArray();
            var playerCount = players.Length;
            Army[] losses = new Army[playerCount];
            for (int i = 0; i < playerCount - 1; i++)
            {
                string player1 = players[i];
                var army1 = fights[coordinates][player1];
                for (int j = i + 1; j < playerCount; j++)
                {
                    string player2 = players[j];
                    if (fights[coordinates].ContainsKey(player2))
                    {
                        var army2 = fights[coordinates][player2];
                        (var army1Losses, var army2Losses) = Fight(army1, army2, fightExponent);
                        losses[i] += army1Losses;
                        losses[j] += army2Losses;
                    }
                }
                Army remainingArmy = armies[player1][coordinates] - losses[i];
                remainingArmy = remainingArmy.Max(Army.Empty);
                armies[players[i]][coordinates] = remainingArmy;
                if (losses[i] < army1)
                {
                    var losers = fights[coordinates].Keys.Where(x => x != player1 && structures[x][coordinates].Any());
                    foreach (var player in losers)
                    {
                        structures[player][coordinates] -= Destroy(remainingArmy, structures[player][coordinates], damage, armor);
                    }
                }
            }
        }
    }

    private static HexMap<PlayerMap<Fight>> GetFights(double fightExponent, Dictionary<string, HexMap<Army>> armiesPerPlayer)
    {
        HashSet<Coordinates> coordinates = armiesPerPlayer.SelectMany(x => x.Value.Keys).ToHashSet();
        Dictionary<Coordinates, ImmutableDictionary<string, Fight>> fights = new();
        foreach (var coordinate in coordinates)
        {
            Dictionary<string, Army> fightingArmies = armiesPerPlayer
                .Where(x => x.Value.ContainsKey(coordinate) && x.Value[coordinate].Sum() > 0)
                .ToDictionary(x => x.Key, x => x.Value[coordinate]);
            if (fightingArmies.Count > 1)
            {
                var fight = fightingArmies.ToImmutableDictionary(x => x.Key, x => x.Value[coordinae]);
                fights.Add(coordinae, fight);
            }
        }
        return new HexMap<PlayerMap<Fight>>(fights.ToDictionary(x => x.Key, x => new PlayerMap<Fight>(x.Value)));
    }

    private static HexMap<Structure> CompleteConstructions(HexMap<Structure> structures, HexMap<Structure> constructions)
    {
        var result = structures.ToDictionary();
        foreach (var coordinates in constructions.Keys)
        {
            result[coordinates] += constructions[coordinates];
        }
        return new HexMap<Structure>(result);
    }

    private static HexMap<Army> CompleteTrainings(HexMap<Army> armies, HexMap<Army> completedTrainings)
    {
        var result = armies.ToDictionary();
        foreach (var coordinates in completedTrainings.Keys)
        {
            result[coordinates] += completedTrainings[coordinates];
        }
        return new HexMap<Army>(result);
    }

    private static HexMap<Army> MoveArmies(HexMap<Army> armyMap, IImmutableSet<Move> moves)
    {
        var result = armyMap.ToDictionary();
        foreach (var move in moves)
        {
            result[move.Origin] -= move.Army;
            Coordinates destination = move.Origin.GetNeighbor(move.Direction);
            result[destination] += move.Army;
        }
        return new HexMap<Army>(result);
    }

    private static TurnMap<HexMap<Structure>> AddConstructions(ushort turn,
       TurnMap<HexMap<Structure>> constructions,
        HexMap<Structure> orderConstructions,
        Structure constructionDuration)
    {
        var buildings = Enum.GetValues(typeof(Building));
        Dictionary<ushort, Dictionary<Coordinates, Structure>> result = new();
        foreach (Coordinates coordinates in orderConstructions.Keys)
        {
            foreach (Building building in buildings)
            {
                if (orderConstructions[coordinates][building] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(turn + constructionDuration[building]);
                    if (!constructions.ContainsKey(completionTurn))
                    {
                        result.Add(completionTurn, new Dictionary<Coordinates, Structure>());
                    }
                    if (!result[completionTurn].ContainsKey(coordinates))
                    {
                        result[completionTurn].Add(coordinates, new Structure());
                    }
                    Structure newStructure = constructions[completionTurn][coordinates].Add(building, orderConstructions[coordinates][building]);
                    result[completionTurn][coordinates] = newStructure;
                }
            }
        }

        return new TurnMap<HexMap<Structure>>(result.ToDictionary(x => x.Key, x => new HexMap<Structure>(x.Value)));
    }

    private static TurnMap<HexMap<Army>> AddTrainings(ushort turn,
        TurnMap<HexMap<Army>> trainings,
        HexMap<Army> orderTrainings,
        Army trainingDuration)
    {
        Dictionary<ushort, Dictionary<Coordinates, Army>> result = new Dictionary<ushort, Dictionary<Coordinates, Army>>();
        var units = Enum.GetValues(typeof(Unit));
        foreach (Coordinates coordinates in orderTrainings.Keys)
        {
            foreach (Unit unit in units)
            {
                if (orderTrainings[coordinates][unit] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(turn + trainingDuration[unit]);
                    if (!trainings.ContainsKey(completionTurn))
                    {
                        result.Add(completionTurn, new Dictionary<Coordinates, Army>());
                    }
                    if (!result[completionTurn].ContainsKey(coordinates))
                    {
                        result[completionTurn].Add(coordinates, new Army());
                    }
                    Army newArmy = trainings[completionTurn][coordinates].Add(unit, orderTrainings[coordinates][unit]);
                    result[completionTurn][coordinates] = newArmy;
                }
            }
        }

        return new TurnMap<HexMap<Army>>(result.ToDictionary(x => x.Key, x => new HexMap<Army>(x.Value)));
    }

    private static short GetConstructionCost(Structure cost, HexMap<Structure> constructions)
    {
        short constructionCosts = 0;
        foreach (var construction in constructions.Values)
        {
            constructionCosts += (construction * cost).Sum();
        }
        return constructionCosts;
    }

    private static short GetTrainingCost(Army cost, HexMap<Army> trainings)
    {
        short trainingCost = 0;
        foreach (var training in trainings.Values)
        {
            trainingCost += (training * cost).Sum();
        }
        return trainingCost;
    }

    private static HexMap<ushort> GetRevenue(HexMap<Army> armyMap, Army incomeSettings, HexMap<ushort> remainingMatter)
    {
        Dictionary<Coordinates, ushort> revenue = new();
        foreach (var coordinate in armyMap.Keys)
        {
            ushort incomeAtHex = Convert.ToUInt16((armyMap[coordinate] * incomeSettings).Sum());
            incomeAtHex = Math.Min(incomeAtHex, Convert.ToUInt16(remainingMatter[coordinate]));
            revenue[coordinate] = incomeAtHex;
        }
        return new HexMap<ushort>(revenue);
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
                    return;
                }
            }
        }
        throw new Exception("The number of players exceed the possible starting hexagons.");
    }
}