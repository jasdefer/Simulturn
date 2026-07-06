namespace Simulturn.Core.Model.State;

/// <summary>
/// Everything a player knows about a single hexagon.
/// Static information (terrain, initial matter) is available through the game settings and not repeated here.
/// </summary>
public record HexagonObservation
{
    /// <summary>
    /// The current visibility of the hexagon for the player.
    /// </summary>
    public required Visibility Visibility { get; init; }

    /// <summary>
    /// The turn on which the hexagon was last at least partially visible, or null if it was never seen.
    /// Equals the current turn while the hexagon is at least partially visible.
    /// </summary>
    public ushort? LastSeenTurn { get; init; }

    /// <summary>
    /// The remaining harvestable matter as of <see cref="LastSeenTurn"/>, or null if the hexagon was never seen.
    /// </summary>
    public int? RemainingMatter { get; init; }

    /// <summary>
    /// The buildings of other players as of <see cref="LastSeenTurn"/>, keyed by player id.
    /// Can be outdated if the hexagon is no longer visible.
    /// </summary>
    public ImmutableDictionary<string, Compound> OpponentCompounds { get; init; } = ImmutableDictionary<string, Compound>.Empty;

    /// <summary>
    /// The total number of units of other players currently on the hexagon, keyed by player id.
    /// Only populated while the hexagon is fully visible. The unit composition remains hidden.
    /// </summary>
    public ImmutableDictionary<string, int> OpponentUnitCounts { get; init; } = ImmutableDictionary<string, int>.Empty;
}
