using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Web.Models;
using System.Collections.Immutable;

namespace Simulturn.Web.Board;

/// <summary>
/// Projects a <see cref="GameState"/> (plus optionally staged draft commands) into a
/// <see cref="BoardView"/>. All visibility filtering happens here — the board components
/// render whatever they are given without any knowledge of fog rules.
/// </summary>
public sealed class BoardProjector
{
    private static readonly ImmutableArray<Unit> _units = Enum.GetValues<Unit>().ToImmutableArray();
    private static readonly ImmutableArray<Building> _buildings = Enum.GetValues<Building>().ToImmutableArray();

    /// <summary>
    /// God mode: every hex fully visible, all players' staged orders included. Passing the
    /// last turn's report overlays what happened: executed movement arrows and loss markers.
    /// </summary>
    public BoardView ProjectGodMode(
        GameState state,
        IReadOnlyDictionary<string, Dictionary<Hexagon, DraftCommand>>? drafts = null,
        TurnReport? lastTurn = null)
    {
        var playerIndexById = GetPlayerIndices(state);
        var lossesByHex = GetLossesByHex(lastTurn, playerIndexById);
        var hexes = SortedHexagons(state)
            .Select(hexagon => ProjectHexGodMode(state, hexagon, playerIndexById, drafts, lossesByHex))
            .ToImmutableArray();
        var arrows = ProjectStagedArrows(playerIndexById, drafts)
            .AddRange(ProjectExecutedArrows(playerIndexById, lastTurn));
        return new BoardView(hexes, arrows, HexLayout.Bounds(state.Hexagons), playerIndexById);
    }

    private static Dictionary<Hexagon, ImmutableArray<LossMarkerView>> GetLossesByHex(
        TurnReport? lastTurn,
        ImmutableDictionary<string, byte> playerIndexById)
    {
        Dictionary<Hexagon, ImmutableArray<LossMarkerView>> lossesByHex = [];
        if (lastTurn is null)
        {
            return lossesByHex;
        }
        foreach (var player in lastTurn.Players)
        {
            if (!playerIndexById.TryGetValue(player.PlayerId, out byte playerIndex))
            {
                continue;
            }
            foreach (var battle in player.Battles)
            {
                var marker = new LossMarkerView(playerIndex, battle.Losses);
                lossesByHex[battle.Hexagon] = lossesByHex.TryGetValue(battle.Hexagon, out var existing)
                    ? existing.Add(marker)
                    : [marker];
            }
        }
        return lossesByHex;
    }

    private static ImmutableArray<OrderArrowView> ProjectExecutedArrows(
        ImmutableDictionary<string, byte> playerIndexById,
        TurnReport? lastTurn)
    {
        if (lastTurn is null)
        {
            return [];
        }
        var arrows = ImmutableArray.CreateBuilder<OrderArrowView>();
        foreach (var player in lastTurn.Players)
        {
            if (!playerIndexById.TryGetValue(player.PlayerId, out byte playerIndex))
            {
                continue;
            }
            foreach (var movement in player.Movements)
            {
                arrows.Add(new OrderArrowView(
                    $"x{playerIndex}:{movement.From.X},{movement.From.Y}>{movement.To.X},{movement.To.Y}",
                    movement.From,
                    movement.To,
                    playerIndex,
                    movement.Army,
                    ArrowKind.ExecutedLastTurn));
            }
        }
        return arrows.ToImmutable();
    }

    /// <summary>
    /// Hot-seat view built from the engine's fog-of-war projection (<see cref="PlayerGameState"/>):
    /// remaining matter and enemy buildings come from possibly stale memories, enemy unit counts
    /// require full visibility, and unit composition is never revealed. The viewer's own content
    /// (armies, queues, staged ghosts) is always fully rendered; enemy staged orders never exist here.
    /// </summary>
    public BoardView ProjectPlayerView(
        PlayerGameState playerView,
        IReadOnlyDictionary<string, Dictionary<Hexagon, DraftCommand>>? drafts = null)
    {
        var playerIndexById = GetPlayerIndices(playerView.PlayerIds);
        var viewerDrafts = drafts?.GetValueOrDefault(playerView.PlayerId);
        var hexes = playerView.Observations.Keys
            .OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X)
            .Select(hexagon => ProjectHexPlayerView(playerView, hexagon, playerIndexById, viewerDrafts))
            .ToImmutableArray();
        var viewerOnlyDrafts = viewerDrafts is null
            ? null
            : new Dictionary<string, Dictionary<Hexagon, DraftCommand>> { [playerView.PlayerId] = viewerDrafts };
        var arrows = ProjectStagedArrows(playerIndexById, viewerOnlyDrafts);
        return new BoardView(hexes, arrows, HexLayout.Bounds(playerView.Observations.Keys), playerIndexById);
    }

    private HexView ProjectHexPlayerView(
        PlayerGameState playerView,
        Hexagon hexagon,
        ImmutableDictionary<string, byte> playerIndexById,
        Dictionary<Hexagon, DraftCommand>? viewerDrafts)
    {
        var hexagonSettings = playerView.GameSettings.HexagonSettings[hexagon];
        var observation = playerView.Observations[hexagon];
        bool stale = observation.LastSeenTurn is { } lastSeen && lastSeen < playerView.Turn;

        var occupants = ImmutableArray.CreateBuilder<HexOccupantView>();
        foreach ((string playerId, byte playerIndex) in playerIndexById.OrderBy(pair => pair.Value))
        {
            if (playerId == playerView.PlayerId)
            {
                var own = ProjectOccupant(playerView.Turn, hexagon, playerId, playerIndex, playerView.PlayerState, viewerDrafts);
                if (own is not null)
                {
                    occupants.Add(own);
                }
                continue;
            }

            Compound enemyCompound = observation.OpponentCompounds.GetValueOrDefault(playerId, Compound.Empty);
            int? enemyUnitCount = observation.OpponentUnitCounts.TryGetValue(playerId, out int count) ? count : null;
            if (!enemyCompound.IsEmpty || enemyUnitCount is not null)
            {
                occupants.Add(new HexOccupantView(playerId, playerIndex, Army: null, enemyUnitCount, enemyCompound,
                    Army.Empty, Army.Empty, Compound.Empty, [], CompoundStale: stale && !enemyCompound.IsEmpty));
            }
        }

        return new HexView(
            hexagon,
            observation.Visibility,
            hexagonSettings.IsBuildable,
            observation.RemainingMatter,
            hexagonSettings.Matter,
            occupants.ToImmutable(),
            UnknownPresence: false,
            HasResearchableUpgrades: !hexagonSettings.ResearchableUpgrades.IsEmpty,
            Losses: [],
            MatterStale: stale);
    }

    /// <summary>
    /// Stable player→color-slot assignment, ordered by player id
    /// (same convention as <c>Simulturn.Core.Helper.Printer</c>).
    /// </summary>
    public static ImmutableDictionary<string, byte> GetPlayerIndices(GameState state) =>
        GetPlayerIndices(state.PlayerIds);

    public static ImmutableDictionary<string, byte> GetPlayerIndices(IEnumerable<string> playerIds)
    {
        byte index = 0;
        return playerIds
            .Order()
            .ToImmutableDictionary(playerId => playerId, _ => index++);
    }

    private static IEnumerable<Hexagon> SortedHexagons(GameState state) =>
        // ImmutableHashSet enumeration order is unstable across instances; sort for stable @key diffing.
        state.Hexagons.OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X);

    private HexView ProjectHexGodMode(
        GameState state,
        Hexagon hexagon,
        ImmutableDictionary<string, byte> playerIndexById,
        IReadOnlyDictionary<string, Dictionary<Hexagon, DraftCommand>>? drafts,
        Dictionary<Hexagon, ImmutableArray<LossMarkerView>> lossesByHex)
    {
        var hexagonSettings = state.GameSettings.HexagonSettings[hexagon];
        var occupants = ImmutableArray.CreateBuilder<HexOccupantView>();
        foreach ((string playerId, byte playerIndex) in playerIndexById.OrderBy(pair => pair.Value))
        {
            var playerState = state.PlayerStates[playerId];
            var playerDrafts = drafts?.GetValueOrDefault(playerId);
            var occupant = ProjectOccupant(state.Turn, hexagon, playerId, playerIndex, playerState, playerDrafts);
            if (occupant is not null)
            {
                occupants.Add(occupant);
            }
        }

        return new HexView(
            hexagon,
            Visibility.Occupied,
            hexagonSettings.IsBuildable,
            state.RemainingMatter.GetValueOrDefault(hexagon),
            hexagonSettings.Matter,
            occupants.ToImmutable(),
            UnknownPresence: false,
            HasResearchableUpgrades: !hexagonSettings.ResearchableUpgrades.IsEmpty,
            Losses: lossesByHex.GetValueOrDefault(hexagon, []));
    }

    private HexOccupantView? ProjectOccupant(
        ushort turn,
        Hexagon hexagon,
        string playerId,
        byte playerIndex,
        PlayerState playerState,
        Dictionary<Hexagon, DraftCommand>? playerDrafts)
    {
        Army army = playerState.Armies.GetValueOrDefault(hexagon, Army.Empty);
        Compound compound = playerState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
        var inProgress = ProjectQueues(turn, hexagon, playerState);

        var draftAtHex = playerDrafts?.GetValueOrDefault(hexagon);
        Army ghostTraining = draftAtHex?.Training.ToArmy() ?? Army.Empty;
        Compound ghostConstruction = draftAtHex?.Construction.ToCompound() ?? Compound.Empty;

        Army ghostInbound = Army.Empty;
        if (playerDrafts is not null)
        {
            foreach ((_, DraftCommand draft) in playerDrafts)
            {
                foreach (var movement in draft.Movements)
                {
                    if (movement.Destination == hexagon)
                    {
                        ghostInbound += movement.Army.ToArmy();
                    }
                }
            }
        }

        if (army.IsEmpty && compound.IsEmpty && inProgress.IsEmpty &&
            ghostTraining.IsEmpty && ghostConstruction.IsEmpty && ghostInbound.IsEmpty)
        {
            return null;
        }

        return new HexOccupantView(
            playerId,
            playerIndex,
            army,
            ArmyCountOnly: null,
            compound,
            ghostInbound,
            ghostTraining,
            ghostConstruction,
            inProgress);
    }

    private static ImmutableArray<QueueItemView> ProjectQueues(ushort turn, Hexagon hexagon, PlayerState playerState)
    {
        var items = ImmutableArray.CreateBuilder<QueueItemView>();

        foreach ((ushort completionTurn, var trainings) in playerState.Trainings.Where(entry => entry.Key >= turn).OrderBy(entry => entry.Key))
        {
            if (!trainings.TryGetValue(hexagon, out var training) || training.IsEmpty)
            {
                continue;
            }
            int turnsRemaining = completionTurn - turn + 1;
            foreach (var unit in _units)
            {
                if (training[unit] > 0)
                {
                    items.Add(new QueueItemView(QueueKind.Training, unit, null, training[unit], turnsRemaining));
                }
            }
        }

        foreach ((ushort completionTurn, var constructions) in playerState.Constructions.Where(entry => entry.Key >= turn).OrderBy(entry => entry.Key))
        {
            if (!constructions.TryGetValue(hexagon, out var construction) || construction.IsEmpty)
            {
                continue;
            }
            int turnsRemaining = completionTurn - turn + 1;
            foreach (var building in _buildings)
            {
                if (construction[building] > 0)
                {
                    items.Add(new QueueItemView(QueueKind.Construction, null, building, construction[building], turnsRemaining));
                }
            }
        }

        foreach ((ushort completionTurn, var researches) in playerState.Researches.Where(entry => entry.Key >= turn).OrderBy(entry => entry.Key))
        {
            if (researches.ContainsKey(hexagon))
            {
                items.Add(new QueueItemView(QueueKind.Research, null, null, 1, completionTurn - turn + 1));
            }
        }

        return items.ToImmutable();
    }

    private static ImmutableArray<OrderArrowView> ProjectStagedArrows(
        ImmutableDictionary<string, byte> playerIndexById,
        IReadOnlyDictionary<string, Dictionary<Hexagon, DraftCommand>>? drafts)
    {
        if (drafts is null)
        {
            return [];
        }
        var arrows = ImmutableArray.CreateBuilder<OrderArrowView>();
        foreach ((string playerId, var playerDrafts) in drafts.OrderBy(pair => pair.Key))
        {
            if (!playerIndexById.TryGetValue(playerId, out byte playerIndex))
            {
                continue;
            }
            foreach ((Hexagon origin, DraftCommand draft) in playerDrafts.OrderBy(pair => pair.Key.Z).ThenBy(pair => pair.Key.X))
            {
                foreach (var movement in draft.Movements)
                {
                    Army army = movement.Army.ToArmy();
                    if (army.IsEmpty)
                    {
                        continue;
                    }
                    arrows.Add(new OrderArrowView(
                        $"{playerIndex}:{origin.X},{origin.Y}>{movement.Destination.X},{movement.Destination.Y}",
                        origin,
                        movement.Destination,
                        playerIndex,
                        army,
                        ArrowKind.Staged));
                }
            }
        }
        return arrows.ToImmutable();
    }
}
