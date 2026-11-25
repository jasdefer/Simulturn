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
/// <param name="Bots">The bots of the game in the order of their pairing. The game is seen from neither.</param>
/// <param name="Map">The map type.</param>
/// <param name="MapSeed">The seed of the map, from 1 to the map seeds of the run.</param>
/// <param name="Seat">The seat of the first bot, 1 is the first player of the settings. The bots after it take the seats after it.</param>
public sealed record ArenaGameKey(PerBot<string> Bots, string Map, int MapSeed, int Seat)
{
    public override string ToString()
    {
        return $"{Bots[0]} on seat {Seat} against {string.Join(" and ", Bots.Skip(1))} on map {Map} with seed {MapSeed}";
    }
}

/// <summary>
/// One game of a run: how it ended and where the matter of its bots went. The course of the
/// game is not kept; <see cref="ArenaStudy.Replay"/> plays it again from the key.
/// </summary>
/// <param name="Key">The game.</param>
/// <param name="Winner">The place of the bot that won among the bots of the key, or null when nobody won: mutual defeat, stall or turn limit.</param>
/// <param name="End">How the match ended.</param>
/// <param name="Turns">The number of turns that were played.</param>
/// <param name="RemainingMatter">The matter left on all hexagons of the map at the end.</param>
/// <param name="Ledgers">The ledgers of the bots of the key at the end of the game.</param>
public sealed record ArenaGameResult(ArenaGameKey Key, int? Winner, MatchEnd End, ushort Turns, int RemainingMatter, PerBot<MatterLedger> Ledgers)
{
    /// <summary>The name of the bot that won, or null when nobody won.</summary>
    public string? WinnerName => Winner is { } bot ? Key.Bots[bot] : null;
}

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
/// <param name="Ledgers">The ledgers of the bots of the key.</param>
public sealed record PlayedTurn(ushort Turn, int RemainingMatter, PerBot<MatterLedger> Ledgers);

/// <summary>
/// The games of one pairing on one map type with the first bot on one seat.
/// </summary>
/// <param name="Bots">The bots of the pairing in its order.</param>
/// <param name="Map">The map type.</param>
/// <param name="Seat">The seat of the first bot, 1 is the first player of the settings.</param>
/// <param name="Wins">The games each bot won.</param>
/// <param name="Draws">The games without a winner: mutual defeat, stall or turn limit.</param>
/// <param name="AverageTurns">The average number of turns of a game.</param>
public sealed record ArenaRow(PerBot<string> Bots, string Map, int Seat, PerBot<int> Wins, int Draws, double AverageTurns)
{
    public int Games => Wins.Sum() + Draws;

    /// <summary>
    /// The win rate of each bot. They do not add up to 1 when a game had no winner.
    /// </summary>
    public PerBot<WinRate> WinRates => [.. Wins.Select(x => WinRate.Of(x, Games))];
}

/// <summary>
/// The share of won games among all games. A draw is not half a win.
/// </summary>
/// <param name="Rate">The share of won games, 0 without a game.</param>
/// <param name="Low">The lower end of the range the true win rate lies in with 95 % certainty.</param>
/// <param name="High">The upper end of the range the true win rate lies in with 95 % certainty.</param>
public sealed record WinRate(double Rate, double Low, double High)
{
    public static WinRate Of(int wins, int games)
    {
        (double low, double high) = WilsonInterval.Of(wins, games);
        return new WinRate(games == 0 ? 0 : (double)wins / games, low, high);
    }
}
