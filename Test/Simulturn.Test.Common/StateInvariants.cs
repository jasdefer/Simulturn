using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Test.Common;

/// <summary>
/// Properties that must hold in every reachable game state, regardless of how it was reached.
/// Returns human readable violations instead of throwing, so tests can assert on an empty list
/// and see every violation at once.
/// </summary>
public static class StateInvariants
{
    /// <summary>
    /// Invariants of a single state: validity, space accounting, visibility and the fog of war view.
    /// </summary>
    public static IReadOnlyList<string> CheckState(GameState state)
    {
        List<string> violations = [];
        GameSettings settings = state.GameSettings;

        foreach (var validation in state.IsValid())
        {
            violations.Add($"IsValid: {validation}");
        }

        foreach ((string playerId, PlayerState player) in state.PlayerStates)
        {
            if (player.Matter < 0)
            {
                violations.Add($"{playerId}: matter is negative ({player.Matter}).");
            }

            int armySpace = player.Armies.Values.Sum(army => army * settings.RequiredSpace);
            int trainingSpace = player.Trainings
                .SelectMany(x => x.Value.Values)
                .Sum(army => army * settings.RequiredSpace);
            if (player.UsedSpace != armySpace + trainingSpace)
            {
                violations.Add($"{playerId}: used space is {player.UsedSpace}, but armies ({armySpace}) and pending trainings ({trainingSpace}) require {armySpace + trainingSpace}.");
            }

            int providedSpace = player.Compounds.Values.Sum(compound => compound * settings.ProvidedSpace);
            if (player.AvailableSpace != providedSpace)
            {
                violations.Add($"{playerId}: available space is {player.AvailableSpace}, but the compounds provide {providedSpace}.");
            }

            var expectedVisibilities = PlayerStateBuilder.GetVisibility(player.Armies,
                state.Hexagons,
                settings.PartialVisibilityRange,
                settings.VisibilityRange);
            foreach (Hexagon hexagon in state.Hexagons)
            {
                Visibility expected = expectedVisibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
                Visibility actual = player.Visibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
                if (expected != actual)
                {
                    violations.Add($"{playerId}: visibility of {hexagon} is {actual}, but the armies imply {expected}.");
                }
            }

            CheckPlayerView(state, playerId, violations);
        }

        return violations;
    }

    /// <summary>
    /// The player view must show exactly what the visibility allows: nothing current about hidden
    /// hexagons, the truth about visible ones.
    /// </summary>
    private static void CheckPlayerView(GameState state, string playerId, List<string> violations)
    {
        PlayerGameState view = state.GetPlayerGameState(playerId);
        PlayerState player = state.PlayerStates[playerId];
        var opponents = state.PlayerStates.Where(x => x.Key != playerId).ToList();
        foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
        {
            Visibility visibility = player.Visibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
            if (observation.Visibility != visibility)
            {
                violations.Add($"{playerId}: the view reports {observation.Visibility} for {hexagon}, the state has {visibility}.");
            }

            if (visibility >= FogOfWar.UnitObservationThreshold)
            {
                var expectedCounts = opponents
                    .Where(x => x.Value.Armies.TryGetValue(hexagon, out var army) && !army.IsEmpty)
                    .ToDictionary(x => x.Key, x => x.Value.Armies[hexagon].Total);
                if (!DictionariesEqual(expectedCounts, observation.OpponentUnitCounts))
                {
                    violations.Add($"{playerId}: the view reports wrong opponent unit counts on visible {hexagon}.");
                }
            }
            else if (observation.OpponentUnitCounts.Count > 0)
            {
                violations.Add($"{playerId}: the view reveals opponent units on {hexagon} with visibility {visibility}.");
            }

            if (visibility >= FogOfWar.StaticObservationThreshold)
            {
                if (observation.LastSeenTurn != state.Turn)
                {
                    violations.Add($"{playerId}: {hexagon} is {visibility} but was last seen on turn {observation.LastSeenTurn?.ToString() ?? "never"} instead of {state.Turn}.");
                }
                if (observation.RemainingMatter != state.RemainingMatter[hexagon])
                {
                    violations.Add($"{playerId}: the view reports {observation.RemainingMatter} remaining matter on {hexagon}, the state has {state.RemainingMatter[hexagon]}.");
                }
                var expectedCompounds = opponents
                    .Where(x => x.Value.Compounds.TryGetValue(hexagon, out var compound) && !compound.IsEmpty)
                    .ToDictionary(x => x.Key, x => x.Value.Compounds[hexagon]);
                if (!DictionariesEqual(expectedCompounds, observation.OpponentCompounds))
                {
                    violations.Add($"{playerId}: the view reports wrong opponent compounds on {hexagon}.");
                }
            }
            else if (observation.LastSeenTurn >= state.Turn)
            {
                violations.Add($"{playerId}: {hexagon} is {visibility} but the view claims it was seen on turn {observation.LastSeenTurn}.");
            }
        }
    }

    /// <summary>
    /// Invariants over one resolved turn: the turn advances, the map and the players stay, and
    /// matter is conserved. The matter the players gained equals the matter harvested from the map
    /// minus the cost of every training, construction and research command.
    /// </summary>
    public static IReadOnlyList<string> CheckTransition<TCommands>(GameState before,
        IReadOnlyDictionary<string, TCommands> commands,
        GameState after)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        List<string> violations = [];
        if (after.Turn != before.Turn + 1)
        {
            violations.Add($"The turn advanced from {before.Turn} to {after.Turn}.");
        }
        if (!after.PlayerIds.SetEquals(before.PlayerIds))
        {
            violations.Add("The set of players changed.");
        }
        if (!after.Hexagons.SetEquals(before.Hexagons))
        {
            violations.Add("The set of hexagons changed.");
        }
        if (!ReferenceEquals(after.GameSettings, before.GameSettings))
        {
            violations.Add("The game settings changed.");
        }

        int harvested = before.RemainingMatter.Values.Sum() - after.RemainingMatter.Values.Sum();
        int totalCost = 0;
        int totalGain = 0;
        foreach (string playerId in before.PlayerIds)
        {
            int cost = commands.TryGetValue(playerId, out var playerCommands)
                ? CostOf(before, playerId, playerCommands)
                : 0;
            int gain = after.PlayerStates[playerId].Matter - before.PlayerStates[playerId].Matter;
            if (gain < -cost)
            {
                violations.Add($"{playerId}: matter changed by {gain}, but the commands cost only {cost}.");
            }
            totalCost += cost;
            totalGain += gain;
        }
        if (totalGain != harvested - totalCost)
        {
            violations.Add($"Matter is not conserved: the players gained {totalGain}, but {harvested} were harvested and {totalCost} were spent.");
        }

        return violations;
    }

    /// <summary>
    /// The matter the commands cost according to the rules, mirroring <see cref="GameState.Validate"/>.
    /// </summary>
    public static int CostOf(GameState state, string playerId, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        GameSettings settings = state.GameSettings;
        PlayerState player = state.PlayerStates[playerId];
        int cost = 0;
        // Every research pays the next free level after the completed and pending ones.
        Dictionary<Upgrade, int> occupiedLevels = player.PendingResearchLevels()
            .ToDictionary(x => x.Key, x => (int)x.Value);
        foreach (Command command in commands.Values)
        {
            cost += command.Training * settings.ArmyCost;
            cost += command.Construction * settings.CompoundCost;
            if (command.Upgrade is Upgrade upgrade)
            {
                int level = player.UpgradeLevels.GetValueOrDefault(upgrade) + occupiedLevels.GetValueOrDefault(upgrade);
                occupiedLevels[upgrade] = occupiedLevels.GetValueOrDefault(upgrade) + 1;
                ImmutableArray<IUpgrade> levels = settings.Upgrades[upgrade];
                if (level < levels.Length)
                {
                    cost += levels[level].Cost;
                }
            }
        }
        return cost;
    }

    private static bool DictionariesEqual<TValue>(IReadOnlyDictionary<string, TValue> expected, IReadOnlyDictionary<string, TValue> actual)
        where TValue : notnull
    {
        return expected.Count == actual.Count &&
            expected.All(x => actual.TryGetValue(x.Key, out var value) && EqualityComparer<TValue>.Default.Equals(x.Value, value));
    }
}
