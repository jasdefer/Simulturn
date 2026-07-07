using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Web.Models;
using System.Collections.Immutable;

namespace Simulturn.Web.Services;

/// <summary>
/// Derives a <see cref="TurnReport"/> from two consecutive game states plus the commands that
/// were resolved between them. Taking the commands avoids reverse-engineering movements (the
/// engine doesn't record them) and duration-1 orders (which never appear in queues).
/// </summary>
public static class TurnReportService
{
    private static readonly ImmutableArray<Unit> _units = Enum.GetValues<Unit>().ToImmutableArray();
    private static readonly ImmutableArray<Building> _buildings = Enum.GetValues<Building>().ToImmutableArray();

    public static TurnReport Build(
        GameState before,
        IReadOnlyDictionary<string, Dictionary<Hexagon, Command>> commands,
        GameState after)
    {
        var players = before.PlayerIds
            .Order()
            .Select(playerId => BuildPlayerReport(before, commands.GetValueOrDefault(playerId), after, playerId))
            .ToImmutableArray();
        return new TurnReport(before.Turn, after.Turn, players);
    }

    private static PlayerTurnReport BuildPlayerReport(
        GameState before,
        Dictionary<Hexagon, Command>? playerCommands,
        GameState after,
        string playerId)
    {
        var beforeState = before.PlayerStates[playerId];
        var afterState = after.PlayerStates[playerId];
        var settings = before.GameSettings;

        // Battles: Losses is reset each resolution, so afterState.Losses is exactly last turn's fights.
        var battles = afterState.Losses
            .Where(loss => !loss.Value.IsEmpty)
            .Select(loss => new BattleEvent(loss.Key, loss.Value))
            .OrderBy(battle => battle.Hexagon.Z).ThenBy(battle => battle.Hexagon.X)
            .ToImmutableArray();

        // Completions: queue entries keyed with the resolved turn, plus duration-1 orders
        // synthesized from the commands (they complete in the same resolution they are issued).
        var completedTrainings = CompletedTrainings(beforeState, playerCommands, before.Turn, settings);
        var completedConstructions = CompletedConstructions(beforeState, playerCommands, before.Turn, settings);

        // Research: compare upgrade levels.
        var completedResearch = afterState.UpgradeLevels
            .Where(pair => pair.Value > beforeState.UpgradeLevels.GetValueOrDefault(pair.Key))
            .Select(pair => new ResearchCompletedEvent(pair.Key, pair.Value))
            .ToImmutableArray();

        // Structures lost: what the player had plus what completed, minus what remains.
        var structuresLost = StructuresLost(beforeState, completedConstructions, afterState);

        var movements = (playerCommands ?? [])
            .SelectMany(command => command.Value.MovementCommands
                .Where(movement => !movement.Army.IsEmpty)
                .Select(movement => new MovementEvent(command.Key, movement.Destination, movement.Army)))
            .ToImmutableArray();

        var income = Income(before, playerId, after);

        return new PlayerTurnReport(
            playerId,
            beforeState.Matter,
            afterState.Matter,
            battles,
            completedTrainings,
            completedConstructions,
            completedResearch,
            structuresLost,
            movements,
            income);
    }

    private static ImmutableArray<TrainingCompletedEvent> CompletedTrainings(
        PlayerState beforeState,
        Dictionary<Hexagon, Command>? playerCommands,
        ushort resolvedTurn,
        GameSettings settings)
    {
        Dictionary<Hexagon, Army> completed = [];
        if (beforeState.Trainings.TryGetValue(resolvedTurn, out var dueTrainings))
        {
            foreach (var (hexagon, army) in dueTrainings)
            {
                completed[hexagon] = completed.GetValueOrDefault(hexagon, Army.Empty) + army;
            }
        }
        foreach (var (hexagon, command) in playerCommands ?? [])
        {
            foreach (var unit in _units)
            {
                if (command.Training[unit] > 0 && settings.TrainingDuration[unit] == 1)
                {
                    completed[hexagon] = completed.GetValueOrDefault(hexagon, Army.Empty)
                        + Army.FromUnit(unit, command.Training[unit]);
                }
            }
        }
        return completed
            .Where(pair => !pair.Value.IsEmpty)
            .Select(pair => new TrainingCompletedEvent(pair.Key, pair.Value))
            .OrderBy(completion => completion.Hexagon.Z).ThenBy(completion => completion.Hexagon.X)
            .ToImmutableArray();
    }

    private static ImmutableArray<ConstructionCompletedEvent> CompletedConstructions(
        PlayerState beforeState,
        Dictionary<Hexagon, Command>? playerCommands,
        ushort resolvedTurn,
        GameSettings settings)
    {
        Dictionary<Hexagon, Compound> completed = [];
        if (beforeState.Constructions.TryGetValue(resolvedTurn, out var dueConstructions))
        {
            foreach (var (hexagon, compound) in dueConstructions)
            {
                completed[hexagon] = completed.GetValueOrDefault(hexagon, Compound.Empty) + compound;
            }
        }
        foreach (var (hexagon, command) in playerCommands ?? [])
        {
            foreach (var building in _buildings)
            {
                if (command.Construction[building] > 0 && settings.ConstructionDuration[building] == 1)
                {
                    completed[hexagon] = completed.GetValueOrDefault(hexagon, Compound.Empty)
                        + Compound.FromBuilding(building, command.Construction[building]);
                }
            }
        }
        return completed
            .Where(pair => !pair.Value.IsEmpty)
            .Select(pair => new ConstructionCompletedEvent(pair.Key, pair.Value))
            .OrderBy(completion => completion.Hexagon.Z).ThenBy(completion => completion.Hexagon.X)
            .ToImmutableArray();
    }

    private static ImmutableArray<StructureLossEvent> StructuresLost(
        PlayerState beforeState,
        ImmutableArray<ConstructionCompletedEvent> completedConstructions,
        PlayerState afterState)
    {
        var losses = ImmutableArray.CreateBuilder<StructureLossEvent>();
        var hexagons = beforeState.Compounds.Keys
            .Concat(completedConstructions.Select(completion => completion.Hexagon))
            .Concat(afterState.Compounds.Keys)
            .Distinct()
            .OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X);
        foreach (var hexagon in hexagons)
        {
            Compound expected = beforeState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
            foreach (var completion in completedConstructions.Where(completion => completion.Hexagon == hexagon))
            {
                expected += completion.Compound;
            }
            Compound actual = afterState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
            Compound lost = expected - actual;
            if (_buildings.Any(building => lost[building] > 0))
            {
                // Clamp negatives (shouldn't occur) so the report never shows negative losses.
                var clamped = Compound.Empty;
                foreach (var building in _buildings)
                {
                    if (lost[building] > 0)
                    {
                        clamped += Compound.FromBuilding(building, lost[building]);
                    }
                }
                losses.Add(new StructureLossEvent(hexagon, clamped));
            }
        }
        return losses.ToImmutable();
    }

    private static ImmutableArray<IncomeEvent> Income(GameState before, string playerId, GameState after)
    {
        // Matter drained per hex, attributed to the player harvesting there (army + Plane in `before`,
        // matching the engine's income phase which runs before movement).
        var income = ImmutableArray.CreateBuilder<IncomeEvent>();
        var beforeState = before.PlayerStates[playerId];
        foreach (var (hexagon, remainingBefore) in before.RemainingMatter.OrderBy(pair => pair.Key.Z).ThenBy(pair => pair.Key.X))
        {
            int drained = remainingBefore - after.RemainingMatter.GetValueOrDefault(hexagon);
            if (drained <= 0)
            {
                continue;
            }
            bool harvestedHere =
                beforeState.Armies.TryGetValue(hexagon, out var army) && !army.IsEmpty &&
                beforeState.Compounds.TryGetValue(hexagon, out var compound) && compound.Plane > 0;
            if (harvestedHere)
            {
                income.Add(new IncomeEvent(hexagon, drained));
            }
        }
        return income.ToImmutable();
    }
}
