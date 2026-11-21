using System.Text.Json.Serialization;
using Simulturn.Bots;
using Simulturn.Core.Analysis;

namespace Simulturn.Lab.Arena;

/// <summary>
/// The result of an arena run: what was played, on which engine, and one row per pairing, map type and seat.
/// </summary>
/// <param name="Options">The options of the run.</param>
/// <param name="EngineVersion">The version of the engine with the commit it was built from, so that runs before and after a rule change are not mixed up.</param>
/// <param name="Rows">The rows in the order of the pairings, the map types and the seats.</param>
/// <param name="Games">
/// Every game of the run in the order it was planned: how it ended and the ledgers of the game,
/// not its course. Not part of the JSON of a run, the games have a file of their own.
/// </param>
public sealed record ArenaResult(
    ArenaOptions Options,
    string EngineVersion,
    ImmutableArray<ArenaRow> Rows,
    [property: JsonIgnore] ImmutableArray<ArenaGameResult> Games);

/// <summary>
/// What names one game of a run. The engine, the maps and the bot seeds follow from it, so the
/// same key always gives the same game as long as the engine and the bots do not change.
/// </summary>
/// <param name="Bot">The bot the game is seen from.</param>
/// <param name="Opponent">The bot it plays against.</param>
/// <param name="Map">The map type.</param>
/// <param name="MapSeed">The seed of the map, from 1 to the map seeds of the run.</param>
/// <param name="Seat">The seat of the bot, 1 is the first player of the settings.</param>
public sealed record ArenaGameKey(string Bot, string Opponent, string Map, int MapSeed, int Seat);

/// <summary>
/// How a game ended for the bot it is seen from.
/// </summary>
public enum ArenaOutcome
{
    /// <summary>The bot won.</summary>
    Win,

    /// <summary>The opponent won.</summary>
    Loss,

    /// <summary>Nobody won: mutual defeat, stall or turn limit.</summary>
    Draw
}

/// <summary>
/// One game of a run: how it ended and where the matter of both bots went. The course of the
/// game is not kept; <see cref="ArenaStudy.Replay"/> plays it again from the key.
/// </summary>
/// <param name="Key">The game.</param>
/// <param name="Outcome">How it ended for the bot of the key.</param>
/// <param name="End">How the match ended.</param>
/// <param name="Turns">The number of turns that were played.</param>
/// <param name="RemainingMatter">The matter left on all hexagons of the map at the end.</param>
/// <param name="Bot">The ledger of the bot of the key at the end of the game.</param>
/// <param name="Opponent">The ledger of its opponent at the end of the game.</param>
public sealed record ArenaGameResult(ArenaGameKey Key, ArenaOutcome Outcome, MatchEnd End, ushort Turns, int RemainingMatter, MatterLedger Bot, MatterLedger Opponent);

/// <summary>
/// A game of the arena with its course: what a run keeps of it, how the match ended and every
/// state it went through. A run drops the course once the game is over, a replay shows it.
/// </summary>
/// <param name="Game">What a run keeps of the game.</param>
/// <param name="Result">How the match ended.</param>
/// <param name="Turns">One entry per state of the game, the start first. The last one holds the ledgers of the game.</param>
public sealed record PlayedGame(ArenaGameResult Game, MatchResult Result, ImmutableArray<PlayedTurn> Turns);

/// <summary>
/// One state of a game. Whatever a study wants to know of every turn is kept here.
/// </summary>
/// <param name="Turn">The turn of the state, 0 is the start.</param>
/// <param name="RemainingMatter">The matter left on all hexagons of the map.</param>
/// <param name="Bot">The ledger of the bot of the key.</param>
/// <param name="Opponent">The ledger of its opponent.</param>
public sealed record PlayedTurn(ushort Turn, int RemainingMatter, MatterLedger Bot, MatterLedger Opponent);

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
