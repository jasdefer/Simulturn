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
        for (int first = 0; first < runBots.Length; first++)
        {
            for (int second = first + 1; second < runBots.Length; second++)
            {
                pairing++;
                foreach (ArenaMap map in runMaps)
                {
                    for (int mapSeed = 1; mapSeed <= options.MapSeeds; mapSeed++)
                    {
                        GameSettings settings = CreateMap(map, mapSeed);
                        foreach (int seat in _seats)
                        {
                            ArenaGameKey key = new([runBots[first].Name, runBots[second].Name], map.Name, mapSeed, seat);
                            games.Add((pairing, new ArenaGame(key, [runBots[first], runBots[second]], settings)));
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
                .GroupBy(x => (x.First.Pairing, x.Second.Key.Bots, x.Second.Key.Map, x.Second.Key.Seat))
                .Select(row => new ArenaRow(
                    row.Key.Bots[0],
                    row.Key.Bots[1],
                    row.Key.Map,
                    row.Key.Seat,
                    Wins: row.Count(x => x.Second.Winner == 0),
                    Losses: row.Count(x => x.Second.Winner == 1),
                    Draws: row.Count(x => x.Second.Winner is null),
                    AverageTurns: row.Average(x => (double)x.Second.Turns)))
        ];
        return new ArenaResult(options, EngineVersion(), rows, [.. results]);
    }

    /// <summary>
    /// Plays one game of a run again, exactly as <see cref="Run"/> plays it, and keeps every turn
    /// of it. Nothing of a run is stored: the key of a game is all it takes.
    /// </summary>
    /// <exception cref="ArgumentException">The key names an unknown bot or map, a map that is not for two players, more or fewer than two bots, or a seat other than 1 and 2.</exception>
    /// <exception cref="ArenaGameException">The game failed, for example because a bot issued invalid commands.</exception>
    public static PlayedGame Replay(ArenaGameKey key, MatchLimits limits, IReadOnlyList<BotEntry> bots, IReadOnlyList<ArenaMap> maps)
    {
        if (!_seats.Contains(key.Seat))
        {
            throw new ArgumentException($"The seat of a game is 1 or 2, not {key.Seat}.", nameof(key));
        }
        if (key.Bots.Count != _seats.Length)
        {
            throw new ArgumentException($"A game of the arena has {_seats.Length} bots, not {key.Bots.Count}.", nameof(key));
        }
        PerBot<BotEntry> gameBots = [.. key.Bots.Select(name => Find(bots, x => x.Name, name, "bot"))];
        ArenaMap map = Find(maps, x => x.Name, key.Map, "map");
        return Play(new ArenaGame(key, gameBots, CreateMap(map, key.MapSeed)), limits);
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
            // The only place that seats the bots: the first bot of the key takes the player of its
            // seat, the bots after it the players after that one.
            ImmutableArray<string> seatOrder = game.Settings.SeatOrder();
            PerBot<string> playerIds = [.. key.Bots.Places.Select(bot => seatOrder[(key.Seat - 1 + bot) % seatOrder.Length])];
            Dictionary<string, IBot> bots = new();
            foreach (int bot in key.Bots.Places)
            {
                bots.Add(playerIds[bot], game.Bots[bot].Create(BotSeed(key.MapSeed, bot)));
            }

            // The match shows the start first, so the first ledgers are the ones of the start.
            ImmutableDictionary<string, MatterLedger>? ledgers = null;
            ImmutableArray<PlayedTurn>.Builder turns = ImmutableArray.CreateBuilder<PlayedTurn>();
            MatchResult match = Match.Play(game.Settings, bots, limits, turn =>
            {
                ImmutableDictionary<string, MatterLedger> after = ledgers is null ? Ledger.Start(turn.State) : Ledger.After(ledgers, turn);
                ledgers = after;
                turns.Add(new PlayedTurn(
                    turn.State.Turn,
                    turn.State.RemainingMatter.Values.Sum(),
                    [.. playerIds.Select(x => after[x])]));
            });
            if (turns.Count == 0)
            {
                throw new InvalidOperationException("The match showed no state to its observer.");
            }

            int? winner = match.WinnerId is null ? null : playerIds.Places.Single(x => playerIds[x] == match.WinnerId);
            PlayedTurn last = turns[^1];
            ArenaGameResult result = new(key, winner, match.End, match.Turns, last.RemainingMatter, last.Ledgers);
            return new PlayedGame(result, match, turns.ToImmutable());
        }
        catch (Exception exception)
        {
            throw new ArenaGameException($"{key}: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// The seed of the bot at a place of its pairing in the games of a map seed, the same on both
    /// seats. The seeds of the bots of a game and of neighbouring map seeds share no pattern:
    /// <see cref="Random"/> gives seeds that lie a fixed distance apart first rolls that depend on
    /// each other, so that one bot would always open with the answer to the other.
    /// </summary>
    public static int BotSeed(int mapSeed, int bot)
    {
        // The finalizer of MurmurHash3: every bit of the input changes every bit of the result.
        uint hash = unchecked((uint)(mapSeed * _seats.Length + bot));
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
    private sealed record ArenaGame(ArenaGameKey Key, PerBot<BotEntry> Bots, GameSettings Settings);

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
