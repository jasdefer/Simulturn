﻿using SimulturnDomain.DataStructures;
using SimulturnDomain.Enums;
using SimulturnDomain.Helper;
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
            PlayerState playerState = new PlayerState(matter, armies, structures, TurnMap<HexMap<Army>>.Empty(), TurnMap<HexMap<Structure>>.Empty(), HexMap<Fight>.Empty());
            playerStates[player] = playerState;
        }
        HexMap<ushort> remainingMatter = new(gameSettings.HexagonSettingsPerCoordinates.Keys.ToDictionary(x => x, x => gameSettings.HexagonSettingsPerCoordinates[x].Matter));
        PlayerMap<PlayerState> stateMap = new(playerStates);
        State state = new(stateMap, remainingMatter);
        return state;
    }

    public static State GetNextState(State state, IReadOnlyDictionary<string, Order> orders, GameSettings gameSettings)
    {
        Dictionary<string, PlayerState> playerStates = [];
        HexMap<ushort> remainingMatter = state.RemainingMatter;
        foreach (var player in state.PlayerStates.Keys)
        {
            Order order = orders[player];
            PlayerState playerState = state.PlayerStates[player];
            HexMap<ushort> revenue = IncomeHelper.GetRevenue(playerState.ArmyMap, gameSettings.ArmySettings.Income, state.RemainingMatter);
            ushort income = IncomeHelper.GetIncome(revenue, playerState.ArmyMap, gameSettings.ArmySettings.RequiredSpace, gameSettings.UpkeepLevels);
            remainingMatter = RemoveMatter(remainingMatter, revenue);
            short trainingCost = GetTrainingCost(gameSettings.ArmySettings.Cost, order.Trainings);
            short constructionCost = GetConstructionCost(gameSettings.StructureSettings.Cost, order.Constructions);
            short matter = Convert.ToInt16(playerState.Matter + income - trainingCost - constructionCost);
            HexMap<Army> armyMap = MoveArmies(playerState.ArmyMap, order.Moves);
            if (playerState.TrainingMap.ContainsKey(0))
            {
                armyMap = CompleteTrainings(armyMap, playerState.TrainingMap[0]);
            }
            var structureMap = playerState.StructureMap;
            if (playerState.ConstructionMap.ContainsKey(0))
            {
                structureMap = CompleteConstructions(structureMap, playerState.ConstructionMap[0]);
            }
            TurnMap<HexMap<Army>> trainings = AddTrainings(playerState.TrainingMap, order.Trainings, gameSettings.ArmySettings.TrainingDuration);
            TurnMap<HexMap<Structure>> constructions = AddConstructions(playerState.ConstructionMap, order.Constructions, gameSettings.StructureSettings.ConstructionDuration);
            PlayerState preFightState = new PlayerState(matter, armyMap, structureMap, trainings, constructions, HexMap<Fight>.Empty());
            playerStates[player] = preFightState;
        }

        HexMap<PlayerMap<Army>> fightingArmies = GetFightingArmies(new PlayerMap<HexMap<Army>>(playerStates.ToDictionary(x => x.Key, x => x.Value.ArmyMap)));
        PlayerMap<HexMap<Fight>> fights = FightHelper.GetFights(gameSettings.FightExponent, fightingArmies);
        foreach (var player in fights.Keys)
        {
            // To Do add losses and subract later, to now if points dies for constructoins
            HexMap<Army> newArmies = RemoveLosses(playerStates[player].ArmyMap, fights[player]);
            HexMap<Structure> destructions = GetDestructions(player, gameSettings.ArmySettings.StructureDamage, gameSettings.StructureSettings.Armor, playerStates[player].StructureMap, fights);
            if (destructions.Any())
            {
                TurnMap<HexMap<Army>> newTrainings = RemoveTrainings(destructions, playerStates[player].TrainingMap, newArmies);
                TurnMap<HexMap<Structure>> newConstructions = RemoveConstructions(destructions, playerStates[player].ConstructionMap, newArmies);
                HexMap<Structure> newStructures = playerStates[player].StructureMap.Subtract(destructions);

                playerStates[player] = playerStates[player] with 
                { 
                    ArmyMap = newArmies,
                    Fights = fights[player],
                    TrainingMap = newTrainings,
                    ConstructionMap = newConstructions,
                    StructureMap = newStructures
                };
            }
            else
            {
                playerStates[player] = playerStates[player] with { ArmyMap = newArmies, Fights = fights[player] };
            }
        }
        PlayerMap<PlayerState> playerStateMap = new(playerStates);
        State newState = new(playerStateMap, remainingMatter);
        return newState;
    }

    private static TurnMap<HexMap<Structure>> RemoveConstructions(HexMap<Structure> destructions, TurnMap<HexMap<Structure>> constructionMap, HexMap<Army> newArmies)
    {
        Dictionary<ushort, Dictionary<Coordinates, Structure>> result = constructionMap.ToDictionary(x => x.Key, x => x.Value.ToDictionary(x => x.Key, x => x.Value));
        foreach (var coordinates in destructions.Keys)
        {

        }

        return new TurnMap<HexMap<Structure>>(result.ToDictionary(x => x.Key, x => new HexMap<Structure>(x.Value)));
    }

    private static TurnMap<HexMap<Army>> RemoveTrainings(HexMap<Structure> destructions, TurnMap<HexMap<Army>> trainingMap, HexMap<Army> newArmies)
    {
        throw new NotImplementedException();
    }

    public static HexMap<Structure> GetDestructions(string player, Army structureDamage, Structure armor, HexMap<Structure> structureMap, PlayerMap<HexMap<Fight>> fights)
    {
        if (!fights.ContainsKey(player))
        {
            return structureMap;
        }

        Dictionary<Coordinates, Structure> result = [];

        foreach (var opponent in fights.Keys.Where(x => x != player && fights.ContainsKey(x)))
        {
            foreach (var coordinates in structureMap.Keys)
            {
                if (fights[player].ContainsKey(coordinates) &&
                    fights[opponent].ContainsKey(coordinates) &&
                    fights[opponent][coordinates].Surviver.Any())
                {
                    result[coordinates] = FightHelper.Destroy(fights[opponent][coordinates].Surviver, result[coordinates], structureDamage, armor);
                }
            }
        }
        return new HexMap<Structure>(result);
    }

    public static HexMap<Army> RemoveLosses(HexMap<Army> armyMap, HexMap<Fight> fightMap)
    {
        var result = armyMap.ToDictionary();
        foreach (var coordinates in fightMap.Keys)
        {
            result[coordinates] -= fightMap[coordinates].Losses;
            if (!result[coordinates].Any())
            {
                result.Remove(coordinates);
            }
        }
        return new HexMap<Army>(result);
    }

    public static HexMap<ushort> RemoveMatter(HexMap<ushort> remainingMatter, HexMap<ushort> revenue)
    {
        var result = remainingMatter.ToDictionary();
        foreach (var coordinates in revenue.Keys)
        {
            result[coordinates] = Convert.ToUInt16(result[coordinates] - revenue[coordinates]);
            if (result[coordinates] <= 0)
            {
                result.Remove(coordinates);
            }
        }
        return new HexMap<ushort>(result);
    }

    public static HexMap<PlayerMap<Army>> GetFightingArmies(PlayerMap<HexMap<Army>> armiesPerPlayer)
    {
        HashSet<Coordinates> coordinates = armiesPerPlayer.Values.SelectMany(x => x.Keys).ToHashSet();
        Dictionary<Coordinates, Dictionary<string, Army>> fights = [];
        foreach (var coordinate in coordinates)
        {
            Dictionary<string, Army> fightingArmies = armiesPerPlayer.Keys
                .Where(x => armiesPerPlayer[x].ContainsKey(coordinate) && armiesPerPlayer[x][coordinate].Sum() > 0)
                .ToDictionary(x => x, x => armiesPerPlayer[x][coordinate]);
            if (fightingArmies.Count > 1)
            {
                fights.Add(coordinate, fightingArmies);
            }
        }
        return new HexMap<PlayerMap<Army>>(fights.ToDictionary(x => x.Key, x => new PlayerMap<Army>(x.Value)));
    }

    public static HexMap<Structure> CompleteConstructions(HexMap<Structure> structures, HexMap<Structure> constructions)
    {
        var result = structures.ToDictionary();
        foreach (var coordinates in constructions.Keys)
        {
            result.AddOrAdd(coordinates, constructions[coordinates]);
        }
        return new HexMap<Structure>(result);
    }

    public static HexMap<Army> CompleteTrainings(HexMap<Army> armies, HexMap<Army> completedTrainings)
    {
        var result = armies.ToDictionary();
        foreach (var coordinates in completedTrainings.Keys)
        {
            result.AddOrAdd(coordinates, completedTrainings[coordinates]);
        }
        return new HexMap<Army>(result);
    }

    public static HexMap<Army> MoveArmies(HexMap<Army> armyMap, IImmutableSet<Move> moves)
    {
        var result = armyMap.ToDictionary();
        foreach (var move in moves)
        {
            result[move.Origin] -= move.Army;
            if (!result[move.Origin].Any())
            {
                result.Remove(move.Origin);
            }
            Coordinates destination = move.Origin.GetNeighbor(move.Direction);
            result.AddOrAdd(destination, move.Army);
        }
        return new HexMap<Army>(result);
    }

    public static TurnMap<HexMap<Structure>> AddConstructions(
       TurnMap<HexMap<Structure>> constructions,
        HexMap<Structure> orderConstructions,
        Structure constructionDuration)
    {
        var buildings = Enum.GetValues(typeof(Building));
        Dictionary<ushort, Dictionary<Coordinates, Structure>> result = constructions
            .Where(x => x.Key > 0)
            .ToDictionary(
                x => Convert.ToUInt16(x.Key - 1),
                x => x.Value.Keys.ToDictionary(y => y, y => x.Value[y])
            );
        foreach (Coordinates coordinates in orderConstructions.Keys)
        {
            foreach (Building building in buildings)
            {
                if (orderConstructions[coordinates][building] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(constructionDuration[building] - 1);
                    if (!result.ContainsKey(completionTurn))
                    {
                        result.Add(completionTurn, []);
                    }
                    if (!result[completionTurn].ContainsKey(coordinates))
                    {
                        result[completionTurn].Add(coordinates, new Structure());
                    }
                    Structure newStructure = result[completionTurn][coordinates].Add(building, orderConstructions[coordinates][building]);
                    result[completionTurn][coordinates] = newStructure;
                }
            }
        }

        return new TurnMap<HexMap<Structure>>(result.ToDictionary(x => x.Key, x => new HexMap<Structure>(x.Value)));
    }

    public static TurnMap<HexMap<Army>> AddTrainings(
        TurnMap<HexMap<Army>> trainings,
        HexMap<Army> orderTrainings,
        Army trainingDuration)
    {
        Dictionary<ushort, Dictionary<Coordinates, Army>> result = trainings
            .Where(x => x.Key > 0)
            .ToDictionary(
                x => Convert.ToUInt16(x.Key - 1),
                x => x.Value.Keys.ToDictionary(y => y, y => x.Value[y])
            );
        var units = Enum.GetValues(typeof(Unit));
        foreach (Coordinates coordinates in orderTrainings.Keys)
        {
            foreach (Unit unit in units)
            {
                if (orderTrainings[coordinates][unit] > 0)
                {
                    ushort completionTurn = Convert.ToUInt16(trainingDuration[unit] - 1);
                    if (!result.ContainsKey(completionTurn))
                    {
                        result.Add(completionTurn, []);
                    }
                    if (!result[completionTurn].ContainsKey(coordinates))
                    {
                        result[completionTurn].Add(coordinates, new Army());
                    }
                    Army newArmy = result[completionTurn][coordinates].Add(unit, orderTrainings[coordinates][unit]);
                    result[completionTurn][coordinates] = newArmy;
                }
            }
        }

        return new TurnMap<HexMap<Army>>(result.ToDictionary(x => x.Key, x => new HexMap<Army>(x.Value)));
    }

    public static short GetConstructionCost(Structure cost, HexMap<Structure> constructions)
    {
        short constructionCosts = 0;
        foreach (var construction in constructions.Values)
        {
            constructionCosts += (construction * cost).Sum();
        }
        return constructionCosts;
    }

    public static short GetTrainingCost(Army cost, HexMap<Army> trainings)
    {
        short trainingCost = 0;
        foreach (var training in trainings.Values)
        {
            trainingCost += (training * cost).Sum();
        }
        return trainingCost;
    }
}