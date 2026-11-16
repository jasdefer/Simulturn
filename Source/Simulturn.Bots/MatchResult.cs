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
/// The outcome of a match.
/// </summary>
/// <param name="End">How the match ended.</param>
/// <param name="WinnerId">The winner of a <see cref="MatchEnd.Victory"/>, otherwise null.</param>
/// <param name="Turns">The number of turns that were played.</param>
public sealed record MatchResult(MatchEnd End, string? WinnerId, ushort Turns);
