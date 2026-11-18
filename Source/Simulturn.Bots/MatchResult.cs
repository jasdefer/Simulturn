namespace Simulturn.Bots;

/// <summary>
/// How a match ended. Only <see cref="Victory"/> has a winner, the others are draws.
/// </summary>
public enum MatchEnd
{
    /// <summary>One player is left with buildings.</summary>
    Victory,

    /// <summary>The last players lost their last buildings in the same turn.</summary>
    MutualDefeat,

    /// <summary>Nothing happened for <see cref="MatchLimits.StallTurns"/> turns in a row.</summary>
    Stall,

    /// <summary>The match reached <see cref="MatchLimits.MaxTurns"/>.</summary>
    TurnLimit
}

/// <summary>
/// The outcome of a match: <see cref="WonBy"/> or <see cref="Drawn"/>. Nothing else builds one,
/// so a result has a winner exactly when it is a <see cref="MatchEnd.Victory"/>.
/// </summary>
public sealed record MatchResult
{
    private MatchResult(MatchEnd end, string? winnerId, ushort turns)
    {
        End = end;
        WinnerId = winnerId;
        Turns = turns;
    }

    /// <summary>How the match ended.</summary>
    public MatchEnd End { get; }

    /// <summary>The winner of a <see cref="MatchEnd.Victory"/>, otherwise null.</summary>
    public string? WinnerId { get; }

    /// <summary>The number of turns that were played.</summary>
    public ushort Turns { get; }

    /// <summary>
    /// A match the player won.
    /// </summary>
    public static MatchResult WonBy(string winnerId, ushort turns)
    {
        return new MatchResult(MatchEnd.Victory, winnerId, turns);
    }

    /// <summary>
    /// A match without a winner: a mutual defeat, a stall or the turn limit.
    /// </summary>
    /// <exception cref="ArgumentException">The end is a victory, which has a winner.</exception>
    public static MatchResult Drawn(MatchEnd end, ushort turns)
    {
        if (end == MatchEnd.Victory)
        {
            throw new ArgumentException($"A victory has a winner, use {nameof(WonBy)}.", nameof(end));
        }
        return new MatchResult(end, null, turns);
    }
}
