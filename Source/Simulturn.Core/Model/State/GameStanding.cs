namespace Simulturn.Core.Model.State;

/// <summary>
/// Who is out of the game and whether the game has ended. Computed by <see cref="Engine.Standing.Of"/>
/// and never stored: it follows from the buildings and the pending constructions alone.
/// Every player may know it, so it is part of every <see cref="PlayerGameState"/>.
/// </summary>
public sealed record GameStanding
{
    /// <summary>
    /// The players without a building and without a pending construction. A defeated player stays
    /// defeated: the armies remain where they stand and fight when attacked, but take no more orders.
    /// </summary>
    public required ImmutableHashSet<string> DefeatedPlayerIds { get; init; }

    public required GameOutcome Outcome { get; init; }

    public bool IsGameOver => Outcome.Value is not GameRunning;
}

/// <summary>
/// The closed set of ways a game stands: still running, won by the last player who is not
/// defeated, or lost by everyone because the last players were defeated in the same turn.
/// </summary>
public union GameOutcome(GameRunning, Victory, MutualDefeat)
{
    public override string ToString() => Value?.ToString() ?? nameof(GameOutcome);
}

/// <summary>
/// At least two players are not defeated.
/// </summary>
public sealed record GameRunning;

/// <summary>
/// Exactly one player is not defeated.
/// </summary>
public sealed record Victory(string WinnerId);

/// <summary>
/// Every player is defeated.
/// </summary>
public sealed record MutualDefeat;
