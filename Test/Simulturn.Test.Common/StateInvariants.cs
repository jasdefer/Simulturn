using Simulturn.Core.Engine;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
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

        foreach (var validation in StateValidator.Validate(state))
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
            int trainingSpace = player.Trainings.Values.Sum(army => army * settings.RequiredSpace);
            if (player.UsedSpace != armySpace + trainingSpace)
            {
                violations.Add($"{playerId}: used space is {player.UsedSpace}, but armies ({armySpace}) and pending trainings ({trainingSpace}) require {armySpace + trainingSpace}.");
            }

            int providedSpace = player.Compounds.Values.Sum(compound => compound * settings.ProvidedSpace);
            if (player.AvailableSpace != providedSpace)
            {
                violations.Add($"{playerId}: available space is {player.AvailableSpace}, but the compounds provide {providedSpace}.");
            }

            CheckPlayerView(state, playerId, violations);
        }

        return violations;
    }

    /// <summary>
    /// The player view must show exactly what the visibility allows: nothing about hidden
    /// hexagons, the truth about visible ones.
    /// </summary>
    private static void CheckPlayerView(GameState state, string playerId, List<string> violations)
    {
        PlayerGameState view = FogOfWar.GetPlayerGameState(state, playerId);
        PlayerState player = state.PlayerStates[playerId];
        var visibilities = FogOfWar.GetVisibility(player.Armies, state.GameSettings);
        var opponents = state.PlayerStates.Where(x => x.Key != playerId).ToList();
        foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
        {
            Visibility visibility = visibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
            if (observation.Visibility != visibility)
            {
                violations.Add($"{playerId}: the view reports {observation.Visibility} for {hexagon}, the armies imply {visibility}.");
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
            else if (observation.RemainingMatter is not null || observation.OpponentCompounds.Count > 0)
            {
                violations.Add($"{playerId}: the view reveals remaining matter or opponent compounds on {hexagon} with visibility {visibility}.");
            }
        }
    }

    /// <summary>
    /// Invariants over one resolved turn: the turn advances, the map and the players stay, and
    /// matter is conserved. The matter the players gained equals the matter harvested from the map
    /// minus the cost of every training, construction and research command.
    /// The report must explain every change between the two states: armies, compounds, matter,
    /// space, pending orders and upgrade levels. The comparison is a check, never the source of the report.
    /// </summary>
    public static IReadOnlyList<string> CheckTransition<TCommands>(GameState before,
        IReadOnlyDictionary<string, TCommands> commands,
        TurnResult result)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        List<string> violations = [];
        GameState after = result.State;
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

        if (violations.Count == 0)
        {
            CheckReport(before, commands, result, violations);
        }
        return violations;
    }

    private static void CheckReport<TCommands>(GameState before,
        IReadOnlyDictionary<string, TCommands> commands,
        TurnResult result,
        List<string> violations)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        GameState after = result.State;
        TurnReport report = result.Report;
        GameSettings settings = before.GameSettings;

        foreach (Hexagon hexagon in before.Hexagons)
        {
            int reported = report.Activities.Values.Sum(x => x.Incomes.TryGetValue(hexagon, out var income) ? income.Matter : 0);
            int harvested = before.RemainingMatter[hexagon] - after.RemainingMatter[hexagon];
            if (reported != harvested)
            {
                violations.Add($"Report: {hexagon} lost {harvested} matter, the reported incomes add up to {reported}.");
            }
        }

        foreach (string playerId in before.PlayerIds)
        {
            if (!report.Activities.TryGetValue(playerId, out PlayerActivityReport? activity))
            {
                violations.Add($"Report: {playerId} has no activity.");
                continue;
            }
            PlayerState playerBefore = before.PlayerStates[playerId];
            PlayerState playerAfter = after.PlayerStates[playerId];
            commands.TryGetValue(playerId, out TCommands? playerCommands);

            if (activity.MatterBefore != playerBefore.Matter || activity.MatterAfter != playerAfter.Matter)
            {
                violations.Add($"Report: {playerId} went from {playerBefore.Matter} to {playerAfter.Matter} matter, reported are {activity.MatterBefore} and {activity.MatterAfter}.");
            }
            int income = activity.Incomes.Values.Sum(x => x.Matter);
            int cost = playerCommands is null ? 0 : CostOf(before, playerId, playerCommands);
            if (playerAfter.Matter - playerBefore.Matter != income - cost)
            {
                violations.Add($"Report: the matter of {playerId} changed by {playerAfter.Matter - playerBefore.Matter}, the reported income is {income} and the commands cost {cost}.");
            }

            Dictionary<Hexagon, Army> moved = [];
            foreach (MovementReport movement in activity.Movements)
            {
                moved[movement.Origin] = moved.GetValueOrDefault(movement.Origin) - movement.Army;
                moved[movement.Destination] = moved.GetValueOrDefault(movement.Destination) + movement.Army;
            }

            Army ordered = Army.Empty;
            Army lost = Army.Empty;
            Army canceledTrainings = Army.Empty;
            Compound built = Compound.Empty;
            Compound destroyed = Compound.Empty;
            foreach (Hexagon hexagon in before.Hexagons)
            {
                Command? command = playerCommands?.GetValueOrDefault(hexagon);
                Army hexagonLost = report.Fights.TryGetValue(hexagon, out FightReport? fight)
                    ? fight.Losses.GetValueOrDefault(playerId)
                    : Army.Empty;
                Compound hexagonDestroyed = Compound.Empty;
                foreach (DestructionReport destruction in report.Destructions)
                {
                    if (destruction.Hexagon == hexagon && destruction.OwnerId == playerId)
                    {
                        hexagonDestroyed += destruction.Buildings;
                    }
                }
                Army hexagonTrained = activity.CompletedTrainings.GetValueOrDefault(hexagon);
                Army hexagonCanceledTrainings = activity.CanceledTrainings.GetValueOrDefault(hexagon);
                Compound hexagonBuilt = activity.CompletedConstructions.GetValueOrDefault(hexagon);
                Army hexagonOrdered = command?.Training ?? Army.Empty;
                ordered += hexagonOrdered;
                lost += hexagonLost;
                canceledTrainings += hexagonCanceledTrainings;
                built += hexagonBuilt;
                destroyed += hexagonDestroyed;

                Army expectedArmy = playerBefore.Armies.GetValueOrDefault(hexagon) + hexagonTrained + moved.GetValueOrDefault(hexagon) - hexagonLost;
                if (playerAfter.Armies.GetValueOrDefault(hexagon) != expectedArmy)
                {
                    violations.Add($"Report: the army of {playerId} on {hexagon} is {playerAfter.Armies.GetValueOrDefault(hexagon)}, the report explains {expectedArmy}.");
                }

                Compound expectedCompound = playerBefore.Compounds.GetValueOrDefault(hexagon) + hexagonBuilt - hexagonDestroyed;
                if (playerAfter.Compounds.GetValueOrDefault(hexagon) != expectedCompound)
                {
                    violations.Add($"Report: the compound of {playerId} on {hexagon} is {playerAfter.Compounds.GetValueOrDefault(hexagon)}, the report explains {expectedCompound}.");
                }

                Army expectedTrainings = playerBefore.Trainings.On(hexagon) + hexagonOrdered - hexagonTrained - hexagonCanceledTrainings;
                if (playerAfter.Trainings.On(hexagon) != expectedTrainings)
                {
                    violations.Add($"Report: the pending trainings of {playerId} on {hexagon} are {playerAfter.Trainings.On(hexagon)}, the report explains {expectedTrainings}.");
                }

                Compound expectedConstructions = playerBefore.Constructions.On(hexagon)
                    + (command?.Construction ?? Compound.Empty)
                    - hexagonBuilt
                    - activity.CanceledConstructions.GetValueOrDefault(hexagon);
                if (playerAfter.Constructions.On(hexagon) != expectedConstructions)
                {
                    violations.Add($"Report: the pending constructions of {playerId} on {hexagon} are {playerAfter.Constructions.On(hexagon)}, the report explains {expectedConstructions}.");
                }

                bool expectedResearch = (playerBefore.Researches.ContainsKey(hexagon) || command?.Research is not null)
                    && !activity.CompletedResearches.ContainsKey(hexagon)
                    && !activity.CanceledResearches.ContainsKey(hexagon);
                if (playerAfter.Researches.ContainsKey(hexagon) != expectedResearch)
                {
                    violations.Add($"Report: {playerId} has {(expectedResearch ? "no" : "a")} pending research on {hexagon}, the report explains the opposite.");
                }
            }

            int expectedUsedSpace = playerBefore.UsedSpace + (ordered - lost - canceledTrainings) * settings.RequiredSpace;
            if (playerAfter.UsedSpace != expectedUsedSpace)
            {
                violations.Add($"Report: the used space of {playerId} is {playerAfter.UsedSpace}, the report explains {expectedUsedSpace}.");
            }
            int expectedAvailableSpace = playerBefore.AvailableSpace + (built - destroyed) * settings.ProvidedSpace;
            if (playerAfter.AvailableSpace != expectedAvailableSpace)
            {
                violations.Add($"Report: the available space of {playerId} is {playerAfter.AvailableSpace}, the report explains {expectedAvailableSpace}.");
            }

            foreach (Upgrade upgrade in playerBefore.UpgradeLevels.Keys.Union(playerAfter.UpgradeLevels.Keys))
            {
                int completed = activity.CompletedResearches.Values.Count(x => x.Upgrade == upgrade);
                int expectedLevel = playerBefore.UpgradeLevels.GetValueOrDefault(upgrade) + completed;
                if (playerAfter.UpgradeLevels.GetValueOrDefault(upgrade) != expectedLevel)
                {
                    violations.Add($"Report: {playerId} has level {playerAfter.UpgradeLevels.GetValueOrDefault(upgrade)} of {upgrade}, the report explains {expectedLevel}.");
                }
            }
            foreach (ResearchReport research in activity.CompletedResearches.Values)
            {
                if (playerAfter.UpgradeLevels.GetValueOrDefault(research.Upgrade) < research.Level)
                {
                    violations.Add($"Report: {playerId} completed level {research.Level} of {research.Upgrade}, the state has level {playerAfter.UpgradeLevels.GetValueOrDefault(research.Upgrade)}.");
                }
            }
        }
    }

    /// <summary>
    /// The matter the commands cost according to the rules, mirroring <see cref="CommandValidator.Validate"/>.
    /// </summary>
    public static int CostOf(GameState state, string playerId, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        GameSettings settings = state.GameSettings;
        PlayerState player = state.PlayerStates[playerId];
        int cost = 0;
        // Every requested upgrade pays the level after the completed ones. Researches never stack,
        // so an upgrade requested twice is invalid and counted once, like in Validate.
        HashSet<Upgrade> requestedUpgrades = [];
        foreach (Command command in commands.Values)
        {
            cost += command.Training * settings.ArmyCost;
            cost += command.Construction * settings.CompoundCost;
            if (command.Research is Upgrade upgrade && requestedUpgrades.Add(upgrade))
            {
                int level = player.UpgradeLevels.GetValueOrDefault(upgrade);
                ImmutableArray<IUpgradeLevel> levels = settings.Upgrades[upgrade];
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
