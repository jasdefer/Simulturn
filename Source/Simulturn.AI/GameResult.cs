using Simulturn.Core.Model.State;

namespace Simulturn.AI;

public record GameResult
{
    public required GameEndReason EndReason { get; init; }

    /// <summary>
    /// The id of the winning player, or null if the game ended in a draw.
    /// </summary>
    public string? WinnerId { get; init; }

    /// <summary>
    /// The number of turns played.
    /// </summary>
    public required ushort Turns { get; init; }

    public required GameState FinalState { get; init; }
}
