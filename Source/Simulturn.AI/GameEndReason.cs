namespace Simulturn.AI;

public enum GameEndReason
{
    /// <summary>
    /// A single player is not defeated; every other player has neither a building nor a pending construction.
    /// </summary>
    Victory,

    /// <summary>
    /// Every player is defeated.
    /// </summary>
    MutualDestruction,

    /// <summary>
    /// The maximum number of turns was reached without a decision. The game is a draw.
    /// </summary>
    TurnLimit
}
