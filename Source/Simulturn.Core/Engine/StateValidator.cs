using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;

namespace Simulturn.Core.Engine;

/// <summary>
/// Checks that a game state is consistent in itself, independent of any commands.
/// </summary>
public static class StateValidator
{
    /// <summary>
    /// Returns every inconsistency of the state; empty when the state is valid.
    /// </summary>
    public static IEnumerable<StateValidation> Validate(GameState state)
    {
        return ValidateRemainingMatter(state)
            .Concat(state.PlayerStates.SelectMany(x => ValidatePlayer(state, x.Key, x.Value)));
    }

    private static IEnumerable<StateValidation> ValidateRemainingMatter(GameState state)
    {
        foreach ((var hexagon, int remainingMatter) in state.RemainingMatter)
        {
            if (remainingMatter < 0)
            {
                yield return new NegativeRemainingMatter(hexagon, remainingMatter);
            }
            if (!state.GameSettings.HexagonSettings.ContainsKey(hexagon))
            {
                yield return new HexagonMissingInSettings(hexagon);
            }
        }
    }

    private static IEnumerable<StateValidation> ValidatePlayer(GameState state, string playerId, PlayerState playerState)
    {
        if (!state.PlayerIds.Contains(playerId))
        {
            yield return new InvalidPlayerId(playerId);
        }

        GameSettings settings = state.GameSettings;
        var validations = ValidateArmies(settings, playerId, playerState)
            .Concat(ValidateCompounds(settings, playerId, playerState))
            .Concat(ValidateTrainings(settings, state.Turn, playerId, playerState))
            .Concat(ValidateConstructions(settings, state.Turn, playerId, playerState))
            .Concat(ValidateResearches(settings, state.Turn, playerId, playerState))
            .Concat(ValidateUpgradeLevels(settings, playerId, playerState));
        foreach (StateValidation validation in validations)
        {
            yield return validation;
        }
    }

    private static IEnumerable<StateValidation> ValidateArmies(GameSettings settings, string playerId, PlayerState playerState)
    {
        foreach ((var hexagon, var army) in playerState.Armies)
        {
            if (!settings.HexagonSettings.ContainsKey(hexagon))
            {
                yield return new HexagonMissingInSettings(hexagon);
            }
            foreach (var unit in EnumValues.Units)
            {
                if (army[unit] < 0)
                {
                    yield return new NegativeUnitCount(playerId, hexagon, unit, army[unit]);
                }
            }
        }
    }

    private static IEnumerable<StateValidation> ValidateCompounds(GameSettings settings, string playerId, PlayerState playerState)
    {
        foreach ((var hexagon, var compound) in playerState.Compounds)
        {
            if (!settings.HexagonSettings.ContainsKey(hexagon))
            {
                yield return new HexagonMissingInSettings(hexagon);
            }
            foreach (var building in EnumValues.Buildings)
            {
                if (compound[building] < 0)
                {
                    yield return new NegativeBuildingCount(playerId, hexagon, building, compound[building]);
                }
            }
        }
    }

    private static IEnumerable<StateValidation> ValidateTrainings(GameSettings settings, ushort currentTurn, string playerId, PlayerState playerState)
    {
        foreach ((TurnHexagon key, Army training) in playerState.Trainings)
        {
            if (key.Turn < currentTurn)
            {
                yield return new ExpiredTraining(playerId, key.Turn);
            }
            if (!settings.HexagonSettings.ContainsKey(key.Hexagon))
            {
                yield return new HexagonMissingInSettings(key.Hexagon);
            }
            foreach (var unit in EnumValues.Units)
            {
                if (training[unit] < 0)
                {
                    yield return new NegativeTrainingCount(playerId, key.Hexagon, unit, training[unit]);
                }
            }
        }
        // Every unit in training needs its own building, whatever its completion turn.
        foreach (Hexagon hexagon in playerState.Trainings.Keys.Select(x => x.Hexagon).Distinct())
        {
            Army trainings = playerState.Trainings.On(hexagon);
            Compound compound = playerState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
            foreach (var unit in EnumValues.Units)
            {
                if (trainings[unit] > compound[GameSettings.TrainingBuilding(unit)])
                {
                    yield return new MissingBuildingForTraining(playerId, hexagon, unit, trainings[unit], compound[GameSettings.TrainingBuilding(unit)]);
                }
            }
        }
    }

    private static IEnumerable<StateValidation> ValidateConstructions(GameSettings settings, ushort currentTurn, string playerId, PlayerState playerState)
    {
        foreach ((TurnHexagon key, Compound construction) in playerState.Constructions)
        {
            if (key.Turn < currentTurn)
            {
                yield return new ExpiredConstruction(playerId, key.Turn);
            }
            if (!settings.HexagonSettings.ContainsKey(key.Hexagon))
            {
                yield return new HexagonMissingInSettings(key.Hexagon);
            }
            foreach (var building in EnumValues.Buildings)
            {
                if (construction[building] < 0)
                {
                    yield return new NegativeConstructionCount(playerId, key.Hexagon, building, construction[building]);
                }
            }
        }
        // Every building under construction needs its own dot, whatever its completion turn.
        foreach (Hexagon hexagon in playerState.Constructions.Keys.Select(x => x.Hexagon).Distinct())
        {
            Compound constructions = playerState.Constructions.On(hexagon);
            short dotCount = playerState.Armies.GetValueOrDefault(hexagon, Army.Empty).Dot;
            if (constructions.Sum() > dotCount)
            {
                yield return new MissingDotsForConstruction(playerId, hexagon, constructions, dotCount);
            }
        }
    }

    private static IEnumerable<StateValidation> ValidateResearches(GameSettings settings, ushort currentTurn, string playerId, PlayerState playerState)
    {
        foreach ((Hexagon hexagon, Research research) in playerState.Researches)
        {
            if (research.CompletionTurn < currentTurn)
            {
                yield return new ExpiredResearch(playerId, research.CompletionTurn);
            }
            if (!settings.HexagonSettings.TryGetValue(hexagon, out var hexagonSettings))
            {
                yield return new HexagonMissingInSettings(hexagon);
            }
            else if (!hexagonSettings.ResearchableUpgrades.Contains(research.Upgrade))
            {
                yield return new UpgradeNotResearchableOnHexagon(playerId, hexagon, research.Upgrade);
            }
            if (!playerState.Armies.TryGetValue(hexagon, out var army) || army.Dot <= 0)
            {
                yield return new ResearchRequiresDot(playerId, hexagon, research.Upgrade);
            }
        }
    }

    /// <summary>
    /// One pending research per upgrade, completed plus pending never exceed the table.
    /// </summary>
    private static IEnumerable<StateValidation> ValidateUpgradeLevels(GameSettings settings, string playerId, PlayerState playerState)
    {
        foreach (Upgrade upgrade in EnumValues.Upgrades)
        {
            byte completedLevels = playerState.UpgradeLevels.GetValueOrDefault(upgrade);
            byte pendingLevels = (byte)playerState.Researches.Values.Count(research => research.Upgrade == upgrade);
            if (pendingLevels > 1)
            {
                yield return new UpgradeInResearchOnMultipleHexagons(playerId, upgrade, pendingLevels);
            }
            int maxLevel = settings.MaxUpgradeLevel(upgrade);
            if (completedLevels + pendingLevels > maxLevel)
            {
                yield return new UpgradeLevelsExceedTable(playerId, upgrade, completedLevels, pendingLevels, maxLevel);
            }
        }
    }
}
