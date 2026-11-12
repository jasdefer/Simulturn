namespace Simulturn.Core.Model.State;

/// <summary>
/// What a player last saw on a hexagon, each part with the turn it was seen. A sighting of
/// the current turn is what the player sees right now, an older one can be outdated.
/// A hexagon that was never seen has no memory.
/// Carried forward by <see cref="Engine.FogOfWar.UpdateMemories"/>; neither the game state
/// nor the view of a turn holds it.
/// </summary>
public sealed record HexagonMemory
{
    /// <summary>
    /// The last sighting of the remaining matter and the buildings.
    /// </summary>
    public required StaticSighting Static { get; init; }

    /// <summary>
    /// The last sighting of the units, or null if the hexagon was never fully visible.
    /// Never newer than <see cref="Static"/>: units need more visibility than buildings.
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
