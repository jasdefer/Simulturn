namespace Simulturn.AI;

public enum GameEndReason
{
    /// <summary>
    /// A single player has buildings left; every other player lost all buildings.
    /// </summary>
    Victory,

    /// <summary>
    /// No player has any buildings left.
    /// </summary>
    MutualDestruction,

    /// <summary>
    /// The maximum number of turns was reached without a decision. The game is a draw.
    /// </summary>
    TurnLimit
}
