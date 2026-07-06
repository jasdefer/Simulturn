namespace Simulturn.Core.Model.State;

/// <summary>
/// What a player remembers about a hexagon from the last turn it was at least partially visible.
/// The values are a snapshot and can be outdated if the hexagon is no longer visible.
/// </summary>
public record HexagonMemory
{
    /// <summary>
    /// The turn on which the hexagon was last at least partially visible.
    /// </summary>
    public required ushort LastSeenTurn { get; init; }

    /// <summary>
    /// The remaining harvestable matter on the hexagon as of <see cref="LastSeenTurn"/>.
    /// </summary>
    public required int RemainingMatter { get; init; }

    /// <summary>
    /// The buildings of other players on the hexagon as of <see cref="LastSeenTurn"/>, keyed by player id.
    /// Only contains players with at least one building on the hexagon.
    /// </summary>
    public required ImmutableDictionary<string, Compound> OpponentCompounds { get; init; }
}
