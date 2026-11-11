namespace Simulturn.Core.Model.State;

/// <summary>
/// What a player last saw on a hexagon in an earlier turn, for the information the current
/// view does not show. Built by <see cref="Engine.FogOfWar.GetMemories"/> from the views of
/// the player; neither the game state nor the view of a turn holds it.
/// The values are snapshots and can be outdated.
/// </summary>
public sealed record HexagonMemory
{
    /// <summary>
    /// The last sighting of the remaining matter and the buildings. Null if they were never
    /// seen, or if the current view shows them.
    /// </summary>
    public StaticSighting? Static { get; init; }

    /// <summary>
    /// The last sighting of the units. Null if they were never seen, or if the current view
    /// shows them.
    /// </summary>
    public UnitSighting? Units { get; init; }
}

/// <summary>
/// The remaining matter and the buildings of a hexagon as of the turn they were last seen.
/// </summary>
public sealed record StaticSighting
{
    public required ushort Turn { get; init; }

    public required int RemainingMatter { get; init; }

    /// <summary>
    /// The buildings of other players, keyed by player id.
    /// Only contains players with at least one building on the hexagon.
    /// </summary>
    public required ImmutableDictionary<string, Compound> OpponentCompounds { get; init; }
}

/// <summary>
/// The units of other players on a hexagon as of the turn they were last seen.
/// </summary>
public sealed record UnitSighting
{
    public required ushort Turn { get; init; }

    /// <summary>
    /// The total number of units of other players, keyed by player id.
    /// Empty if the hexagon was seen without opponent units.
    /// </summary>
    public required ImmutableDictionary<string, int> OpponentUnitCounts { get; init; }
}
