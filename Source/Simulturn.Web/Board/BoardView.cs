using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.Web.Board;

/// <summary>
/// Everything the board renders, projected from a <c>GameState</c> by <see cref="BoardProjector"/>.
/// Fog-of-war filtering happens during projection so the markup can never leak hidden information.
/// </summary>
public sealed record BoardView(
    ImmutableArray<HexView> Hexes,
    ImmutableArray<OrderArrowView> Arrows,
    BoardBounds Bounds,
    ImmutableDictionary<string, byte> PlayerIndexById);

/// <summary>One hexagon as seen by the viewer.</summary>
public sealed record HexView(
    Hexagon Hex,
    Visibility Visibility,
    bool IsBuildable,
    int? RemainingMatter,
    int InitialMatter,
    ImmutableArray<HexOccupantView> Occupants,
    bool UnknownPresence,
    bool HasResearchableUpgrades,
    ImmutableArray<LossMarkerView> Losses,
    bool MatterStale = false);

/// <summary>One player's presence on a hexagon, already fog-filtered.</summary>
public sealed record HexOccupantView(
    string PlayerId,
    byte PlayerIndex,
    Army? Army,
    int? ArmyCountOnly,
    Compound Compound,
    Army GhostInboundArmy,
    Army GhostTraining,
    Compound GhostConstruction,
    ImmutableArray<QueueItemView> InProgress,
    bool CompoundStale = false)
{
    public bool HasArmyContent => Army is { IsEmpty: false } || ArmyCountOnly is > 0 || !GhostInboundArmy.IsEmpty || !GhostTraining.IsEmpty;
    public bool HasBuildingContent => !Compound.IsEmpty || !GhostConstruction.IsEmpty;
}

public enum QueueKind
{
    Training,
    Construction,
    Research,
}

/// <summary>An in-progress queue entry shown on the tile (e.g. a pyramid under construction, 2 turns left).</summary>
public sealed record QueueItemView(QueueKind Kind, Unit? Unit, Building? Building, short Count, int TurnsRemaining);

public sealed record LossMarkerView(byte PlayerIndex, Army Losses);

public enum ArrowKind
{
    Staged,
    ExecutedLastTurn,
}

/// <summary>A movement order rendered as an arrow. Id is stable and identifies the order on click.</summary>
public sealed record OrderArrowView(
    string Id,
    Hexagon From,
    Hexagon To,
    byte PlayerIndex,
    Army Army,
    ArrowKind Kind);
