namespace Simulturn.Web.Models;

public enum GameMode
{
    /// <summary>See everything and issue commands for every player — the game-logic testing mode.</summary>
    God,

    /// <summary>Pass-and-play on one machine with fog of war and a hand-off screen between players.</summary>
    HotSeat,
}
