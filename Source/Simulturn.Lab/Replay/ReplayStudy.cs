using Simulturn.Bots;
using Simulturn.Bots.Catalog;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Replay;

/// <summary>
/// Plays one game of an arena run again and keeps the matter ledger of both players for every
/// turn: the table to look at when a win rate says that a bot loses, but not why.
/// </summary>
public static class ReplayStudy
{
    /// <summary>
    /// Plays the game of the key with the limits of the run it comes from.
    /// </summary>
    /// <exception cref="ArgumentException">The key names an unknown bot or map, a map that is not for two players, or a seat other than 1 and 2.</exception>
    /// <exception cref="ArenaGameException">The game failed, for example because a bot issued invalid commands.</exception>
    public static ReplayResult Run(ArenaGameKey key, MatchLimits limits, IReadOnlyList<BotEntry> bots, IReadOnlyList<ArenaMap> maps)
    {
        List<(ushort Turn, int RemainingMatter, ImmutableDictionary<string, MatterLedger> Ledgers)> states = [];
        ArenaReplay replay = ArenaStudy.Replay(key, limits, bots, maps, turn =>
        {
            var ledgers = states.Count == 0 ? Ledger.Start(turn.State) : Ledger.After(states[^1].Ledgers, turn);
            states.Add((turn.State.Turn, turn.State.RemainingMatter.Values.Sum(), ledgers));
        });

        return new ReplayResult(key, replay.Result,
        [
            .. states.Select(x => new ReplayTurn(x.Turn, x.RemainingMatter, x.Ledgers[replay.BotPlayerId], x.Ledgers[replay.OpponentPlayerId]))
        ]);
    }
}

/// <summary>
/// A game played again.
/// </summary>
/// <param name="Key">The game.</param>
/// <param name="Result">How it ended.</param>
/// <param name="Turns">One entry per state of the game, the start first. The last one holds the ledgers of the game.</param>
public sealed record ReplayResult(ArenaGameKey Key, MatchResult Result, ImmutableArray<ReplayTurn> Turns);

/// <summary>
/// One state of a game played again.
/// </summary>
/// <param name="Turn">The turn of the state, 0 is the start.</param>
/// <param name="RemainingMatter">The matter left on all hexagons of the map.</param>
/// <param name="Bot">The ledger of the bot of the key.</param>
/// <param name="Opponent">The ledger of its opponent.</param>
public sealed record ReplayTurn(ushort Turn, int RemainingMatter, MatterLedger Bot, MatterLedger Opponent);
