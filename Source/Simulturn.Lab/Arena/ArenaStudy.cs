using System.Reflection;
using Simulturn.Bots;
using Simulturn.Bots.Catalog;
using Simulturn.Core.Analysis;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;

namespace Simulturn.Lab.Arena;

/// <summary>
/// Lets bots play against each other and counts who wins: every pairing on every map with both
/// seat orders, so that neither the map nor the seat favours a bot unnoticed. Of every game it
/// keeps how it ended and the ledgers of both bots.
/// </summary>
public static class ArenaStudy
{
    private static readonly ImmutableArray<int> _seats = [1, 2];

    /// <summary>
    /// Plays the run of the options. The bots and the map types are looked up by name in the
    /// given lists, so a new bot or map needs no change here.
    /// </summary>
    /// <exception cref="ArgumentException">The options name fewer than two bots, no map, an unknown bot or map, or a map that is not for two players.</exception>
    /// <exception cref="ArenaGameException">A game of the run failed, for example because a bot issued invalid commands.</exception>
    public static ArenaResult Run(ArenaOptions options, IReadOnlyList<BotEntry> bots, IReadOnlyList<ArenaMap> maps)
    {
        if (options.Bots.Length < 2)
        {
            throw new ArgumentException("An arena run needs at least two bots.", nameof(options));
        }
        if (options.Maps.IsEmpty)
        {
            throw new ArgumentException("An arena run needs at least one map.", nameof(options));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MapSeeds, 1, nameof(options));

        ImmutableArray<BotEntry> runBots = [.. options.Bots.Select(name => Find(bots, x => x.Name, name, "bot"))];
        ImmutableArray<ArenaMap> runMaps = [.. options.Maps.Select(name => Find(maps, x => x.Name, name, "map"))];

        // The number of the pairing keeps two pairings of bots with the same names apart.
        List<(int Pairing, ArenaGame Game)> games = [];
        int pairing = 0;
        for (int bot = 0; bot < runBots.Length; bot++)
        {
            for (int opponent = bot + 1; opponent < runBots.Length; opponent++)
            {
                pairing++;
                foreach (ArenaMap map in runMaps)
                {
                    for (int mapSeed = 1; mapSeed <= options.MapSeeds; mapSeed++)
                    {
                        GameSettings settings = CreateMap(map, mapSeed);
                        foreach (int seat in _seats)
                        {
                            ArenaGameKey key = new(runBots[bot].Name, runBots[opponent].Name, map.Name, mapSeed, seat);
                            games.Add((pairing, new ArenaGame(key, runBots[bot], runBots[opponent], settings)));
                        }
                    }
                }
            }
        }

        ArenaGameResult[] results = new ArenaGameResult[games.Count];
        try
        {
            Parallel.For(0, games.Count, index => results[index] = Play(games[index].Game, options.Limits).Game);
        }
        catch (AggregateException exception) when (exception.InnerExceptions.OfType<ArenaGameException>().FirstOrDefault() is { } failed)
        {
            throw failed;
        }

        // The games of a row lie next to each other, in the order the rows are reported.
        ImmutableArray<ArenaRow> rows =
        [
            .. games.Zip(results)
                .GroupBy(x => (x.First.Pairing, x.Second.Key.Bot, x.Second.Key.Opponent, x.Second.Key.Map, x.Second.Key.Seat))
                .Select(row => new ArenaRow(
                    row.Key.Bot,
                    row.Key.Opponent,
                    row.Key.Map,
                    row.Key.Seat,
                    Wins: row.Count(x => x.Second.Outcome == ArenaOutcome.Win),
                    Losses: row.Count(x => x.Second.Outcome == ArenaOutcome.Loss),
                    Draws: row.Count(x => x.Second.Outcome == ArenaOutcome.Draw),
                    AverageTurns: row.Average(x => (double)x.Second.Turns)))
        ];
        return new ArenaResult(options, EngineVersion(), rows, [.. results]);
    }

    /// <summary>
    /// Plays one game of a run again, exactly as <see cref="Run"/> plays it, and keeps every turn
    /// of it. Nothing of a run is stored: the key of a game is all it takes.
    /// </summary>
    /// <exception cref="ArgumentException">The key names an unknown bot or map, a map that is not for two players, or a seat other than 1 and 2.</exception>
    /// <exception cref="ArenaGameException">The game failed, for example because a bot issued invalid commands.</exception>
    public static PlayedGame Replay(ArenaGameKey key, MatchLimits limits, IReadOnlyList<BotEntry> bots, IReadOnlyList<ArenaMap> maps)
    {
        if (!_seats.Contains(key.Seat))
        {
            throw new ArgumentException($"The seat of a game is 1 or 2, not {key.Seat}.", nameof(key));
        }
        BotEntry bot = Find(bots, x => x.Name, key.Bot, "bot");
        BotEntry opponent = Find(bots, x => x.Name, key.Opponent, "bot");
        ArenaMap map = Find(maps, x => x.Name, key.Map, "map");
        return Play(new ArenaGame(key, bot, opponent, CreateMap(map, key.MapSeed)), limits);
    }

    /// <summary>
    /// The only place of the lab that plays a game: it keeps the ledgers of both bots for every
    /// turn on the way. A run keeps the end of it, a replay all of it.
    /// </summary>
    private static PlayedGame Play(ArenaGame game, MatchLimits limits)
    {
        ArenaGameKey key = game.Key;
        try
        {
            // The only place that seats the bots: the bot of the key takes the player of its seat,
            // the opponent the other one.
            ImmutableArray<string> seatOrder = game.Settings.SeatOrder();
            string botPlayerId = seatOrder[key.Seat - 1];
            string opponentPlayerId = seatOrder[_seats.Length - key.Seat];
            Dictionary<string, IBot> bots = new()
            {
                { botPlayerId, game.Bot.Create(BotSeed(key.MapSeed, opponent: false)) },
                { opponentPlayerId, game.Opponent.Create(BotSeed(key.MapSeed, opponent: true)) }
            };

            // The match shows the start first, so the first ledgers are the ones of the start.
            ImmutableDictionary<string, MatterLedger>? ledgers = null;
            ImmutableArray<PlayedTurn>.Builder turns = ImmutableArray.CreateBuilder<PlayedTurn>();
            MatchResult match = Match.Play(game.Settings, bots, limits, turn =>
            {
                ledgers = ledgers is null ? Ledger.Start(turn.State) : Ledger.After(ledgers, turn);
                turns.Add(new PlayedTurn(
                    turn.State.Turn,
                    turn.State.RemainingMatter.Values.Sum(),
                    ledgers[botPlayerId],
                    ledgers[opponentPlayerId]));
            });
            if (turns.Count == 0)
            {
                throw new InvalidOperationException("The match showed no state to its observer.");
            }

            ArenaOutcome outcome = match.WinnerId is null
                ? ArenaOutcome.Draw
                : match.WinnerId == botPlayerId ? ArenaOutcome.Win : ArenaOutcome.Loss;
            PlayedTurn last = turns[^1];
            ArenaGameResult result = new(
                key,
                outcome,
                match.End,
                match.Turns,
                last.RemainingMatter,
                last.Bot,
                last.Opponent);
            return new PlayedGame(result, match, turns.ToImmutable());
        }
        catch (Exception exception)
        {
            throw new ArenaGameException(
                $"{key.Bot} on seat {key.Seat} against {key.Opponent} on map {key.Map} with seed {key.MapSeed}: {exception.Message}",
                exception);
        }
    }

    /// <summary>
    /// The seed of a bot in the games of a map seed, the same on both seats. The seeds of the two
    /// bots and of neighbouring map seeds share no pattern: <see cref="Random"/> gives seeds that
    /// lie a fixed distance apart first rolls that depend on each other, so that one bot would
    /// always open with the answer to the other.
    /// </summary>
    public static int BotSeed(int mapSeed, bool opponent)
    {
        // The finalizer of MurmurHash3: every bit of the input changes every bit of the result.
        uint hash = unchecked((uint)(mapSeed * 2 + (opponent ? 1 : 0)));
        hash ^= hash >> 16;
        hash *= 0x85EBCA6B;
        hash ^= hash >> 13;
        hash *= 0xC2B2AE35;
        hash ^= hash >> 16;
        return unchecked((int)hash);
    }

    private static T Find<T>(IReadOnlyList<T> items, Func<T, string> name, string wanted, string kind)
    {
        foreach (T item in items)
        {
            if (name(item) == wanted)
            {
                return item;
            }
        }
        throw new ArgumentException($"There is no {kind} named '{wanted}'. Known: {string.Join(", ", items.Select(name))}.");
    }

    private static string EngineVersion()
    {
        Assembly engine = typeof(Game).Assembly;
        return engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? engine.GetName().Version?.ToString()
            ?? "unknown";
    }

    /// <summary>
    /// A game that can be played: its key with what the names of the key stand for. Which player
    /// each bot is follows from the seat of the key and the settings, <see cref="Play"/> works it out.
    /// </summary>
    private sealed record ArenaGame(ArenaGameKey Key, BotEntry Bot, BotEntry Opponent, GameSettings Settings);

    /// <summary>
    /// The settings of a map for a seed. The arena plays games of two players only.
    /// </summary>
    private static GameSettings CreateMap(ArenaMap map, int mapSeed)
    {
        GameSettings settings = map.Create(mapSeed);
        int players = settings.SeatOrder().Length;
        if (players != _seats.Length)
        {
            throw new ArgumentException($"The arena plays games of two players, the map {map.Name} has {players}.");
        }
        return settings;
    }
}

/// <summary>
/// A game of an arena run failed. The message names the game, so that it can be played again.
/// </summary>
public sealed class ArenaGameException(string message, Exception innerException) : Exception(message, innerException);
