namespace Simulturn.Core.Model.Report;

/// <summary>
/// Everything that happened while one turn was resolved, each fact exactly once and without
/// fog of war. Written by the resolution at the place where the event happens, never derived
/// from a comparison of two states. It is not stored: whoever holds the commands of all turns
/// gets it again by resolving them.
/// </summary>
public sealed record TurnReport
{
    public static TurnReport Empty { get; } = new();

    /// <summary>
    /// What happened to each player alone, keyed by player id.
    /// </summary>
    public ImmutableDictionary<string, PlayerActivityReport> Activities { get; init; } = ImmutableDictionary<string, PlayerActivityReport>.Empty;

    /// <summary>
    /// The fights of the turn. A hexagon hosts at most one fight per turn, between all armies on it.
    /// </summary>
    public ImmutableDictionary<Hexagon, FightReport> Fights { get; init; } = ImmutableDictionary<Hexagon, FightReport>.Empty;

    /// <summary>
    /// The destroyed buildings of the turn, one entry per hexagon, attacker and owner.
    /// </summary>
    public ImmutableArray<DestructionReport> Destructions { get; init; } = [];
}
