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

        List<ArenaGame> games = [];
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
                        GameSettings settings = map.Create(mapSeed);
                        (string first, string second) = SeatsOf(settings, map.Name);
                        games.Add(new ArenaGame(pairing, runBots[bot], runBots[opponent], map.Name, mapSeed, 1, first, second, settings));
                        games.Add(new ArenaGame(pairing, runBots[bot], runBots[opponent], map.Name, mapSeed, 2, second, first, settings));
                    }
                }
            }
        }

        ArenaGameResult[] results = new ArenaGameResult[games.Count];
        try
        {
            Parallel.For(0, games.Count, index => results[index] = Play(games[index], options.Limits).Game);
        }
        catch (AggregateException exception) when (exception.InnerExceptions.OfType<ArenaGameException>().FirstOrDefault() is { } failed)
        {
            throw failed;
        }

        // The games of a row lie next to each other, in the order the rows are reported.
        ImmutableArray<ArenaRow> rows =
        [
            .. games.Zip(results)
                .GroupBy(x => (x.First.Pairing, x.First.Bot, x.First.Opponent, x.First.Map, x.First.Seat))
                .Select(row => new ArenaRow(
                    row.Key.Bot.Name,
                    row.Key.Opponent.Name,
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
        if (key.Seat is not (1 or 2))
        {
            throw new ArgumentException($"The seat of a game is 1 or 2, not {key.Seat}.", nameof(key));
        }
        BotEntry bot = Find(bots, x => x.Name, key.Bot, "bot");
        BotEntry opponent = Find(bots, x => x.Name, key.Opponent, "bot");
        ArenaMap map = Find(maps, x => x.Name, key.Map, "map");
        GameSettings settings = map.Create(key.MapSeed);
        (string first, string second) = SeatsOf(settings, map.Name);
        ArenaGame game = key.Seat == 1
            ? new ArenaGame(0, bot, opponent, map.Name, key.MapSeed, 1, first, second, settings)
            : new ArenaGame(0, bot, opponent, map.Name, key.MapSeed, 2, second, first, settings);
        return Play(game, limits);
    }

    /// <summary>
    /// The only place of the lab that plays a game: it keeps the ledgers of both bots for every
    /// turn on the way. A run keeps the end of it, a replay all of it.
    /// </summary>
    private static PlayedGame Play(ArenaGame game, MatchLimits limits)
    {
        try
        {
            Dictionary<string, IBot> bots = new()
            {
                { game.BotPlayerId, game.Bot.Create(BotSeed(game.MapSeed, opponent: false)) },
                { game.OpponentPlayerId, game.Opponent.Create(BotSeed(game.MapSeed, opponent: true)) }
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
                    ledgers[game.BotPlayerId],
                    ledgers[game.OpponentPlayerId]));
            });
            if (turns.Count == 0)
            {
                throw new InvalidOperationException("The match showed no state to its observer.");
            }

            ArenaOutcome outcome = match.WinnerId is null
                ? ArenaOutcome.Draw
                : match.WinnerId == game.BotPlayerId ? ArenaOutcome.Win : ArenaOutcome.Loss;
            PlayedTurn last = turns[^1];
            ArenaGameResult result = new(
                new ArenaGameKey(game.Bot.Name, game.Opponent.Name, game.Map, game.MapSeed, game.Seat),
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
                $"{game.Bot.Name} on seat {game.Seat} against {game.Opponent.Name} on map {game.Map} with seed {game.MapSeed}: {exception.Message}",
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
    /// One game of a run, with the number of its pairing. The bot sits on the given seat, the opponent on the other one.
    /// </summary>
    private sealed record ArenaGame(int Pairing, BotEntry Bot, BotEntry Opponent, string Map, int MapSeed, int Seat,
        string BotPlayerId, string OpponentPlayerId, GameSettings Settings);

    /// <summary>
    /// The players of the two seats of a map, seat 1 first.
    /// </summary>
    private static (string First, string Second) SeatsOf(GameSettings settings, string map)
    {
        ImmutableArray<string> playerIds = [.. Game.Start(settings).State.PlayerIds.Order(StringComparer.Ordinal)];
        if (playerIds.Length != 2)
        {
            throw new ArgumentException($"The arena plays games of two players, the map {map} has {playerIds.Length}.");
        }
        return (playerIds[0], playerIds[1]);
    }
}

/// <summary>
/// A game of an arena run failed. The message names the game, so that it can be played again.
/// </summary>
public sealed class ArenaGameException(string message, Exception innerException) : Exception(message, innerException);
