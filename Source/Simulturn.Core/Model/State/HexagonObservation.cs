namespace Simulturn.Core.Model.State;

/// <summary>
/// What a player sees on a single hexagon in the current turn. Nothing from earlier turns:
/// what was seen before is carried in the memories (<see cref="Engine.FogOfWar.UpdateMemories"/>).
/// Static information (terrain, initial matter) is available through the game settings and not repeated here.
/// </summary>
public sealed record HexagonObservation
{
    /// <summary>
    /// The current visibility of the hexagon for the player.
    /// </summary>
    public required Visibility Visibility { get; init; }

    /// <summary>
    /// The remaining harvestable matter, or null if the hexagon is not at least partially visible.
    /// </summary>
    public int? RemainingMatter { get; init; }

    /// <summary>
    /// The buildings of other players, keyed by player id.
    /// Only populated while the hexagon is at least partially visible.
    /// </summary>
    public ImmutableDictionary<string, Compound> OpponentCompounds { get; init; } = ImmutableDictionary<string, Compound>.Empty;

    /// <summary>
    /// The total number of units of other players currently on the hexagon, keyed by player id.
    /// Only populated while the hexagon is fully visible. The unit composition remains hidden.
    /// </summary>
    public ImmutableDictionary<string, int> OpponentUnitCounts { get; init; } = ImmutableDictionary<string, int>.Empty;
}
