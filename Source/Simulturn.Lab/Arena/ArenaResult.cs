namespace Simulturn.Lab.Arena;

/// <summary>
/// The result of an arena run: what was played, on which engine, and one row per pairing, map type and seat.
/// </summary>
/// <param name="Options">The options of the run.</param>
/// <param name="EngineVersion">The version of the engine with the commit it was built from, so that runs before and after a rule change are not mixed up.</param>
/// <param name="Rows">The rows in the order of the pairings, the map types and the seats.</param>
public sealed record ArenaResult(ArenaOptions Options, string EngineVersion, ImmutableArray<ArenaRow> Rows);

/// <summary>
/// The games of one bot against one opponent on one map type from one seat, seen from the bot.
/// </summary>
/// <param name="Bot">The bot the row counts for.</param>
/// <param name="Opponent">The bot it played against.</param>
/// <param name="Map">The map type.</param>
/// <param name="Seat">The seat of the bot, 1 is the first player of the settings.</param>
/// <param name="Wins">The games the bot won.</param>
/// <param name="Losses">The games the opponent won.</param>
/// <param name="Draws">The games without a winner: mutual defeat, stall or turn limit.</param>
/// <param name="AverageTurns">The average number of turns of a game.</param>
public sealed record ArenaRow(string Bot, string Opponent, string Map, int Seat, int Wins, int Losses, int Draws, double AverageTurns)
{
    public int Games => Wins + Losses + Draws;

    /// <summary>
    /// The share of won games among all games. A draw is not half a win.
    /// </summary>
    public double WinRate => Games == 0 ? 0 : (double)Wins / Games;

    /// <summary>
    /// The lower end of the range the true win rate lies in with 95 % certainty.
    /// </summary>
    public double WinRateLow => WilsonInterval.Of(Wins, Games).Low;

    /// <summary>
    /// The upper end of the range the true win rate lies in with 95 % certainty.
    /// </summary>
    public double WinRateHigh => WilsonInterval.Of(Wins, Games).High;
}

/// <summary>
/// The Wilson score interval of a rate: how far the true rate may lie from the measured one.
/// Few games give a wide range, so 55 % after 40 games says nothing.
/// </summary>
public static class WilsonInterval
{
    private const double Z95 = 1.959964;

    public static (double Low, double High) Of(int successes, int trials)
    {
        if (trials == 0)
        {
            return (0, 1);
        }
        double rate = (double)successes / trials;
        double z2 = Z95 * Z95;
        double denominator = 1 + z2 / trials;
        double center = (rate + z2 / (2 * trials)) / denominator;
        double margin = Z95 * Math.Sqrt(rate * (1 - rate) / trials + z2 / (4.0 * trials * trials)) / denominator;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }
}
