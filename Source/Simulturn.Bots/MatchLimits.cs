namespace Simulturn.Bots;

/// <summary>
/// When a match ends without a decision. The rules of the game know no limit, so two bots
/// that never attack each other would play forever.
/// </summary>
public sealed record MatchLimits
{
    /// <summary>
    /// The match ends after this many turns, whatever happens. The net for bots that keep
    /// doing something without ever deciding the game.
    /// </summary>
    public ushort MaxTurns { get; init; } = 5000;

    /// <summary>
    /// The match ends after this many turns in a row in which nothing happened,
    /// see <see cref="Match.IsStill"/>.
    /// </summary>
    public ushort StallTurns { get; init; } = 1000;
}
