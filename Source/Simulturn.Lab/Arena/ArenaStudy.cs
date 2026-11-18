using System.Reflection;
using Simulturn.Bots;
using Simulturn.Bots.Catalog;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;

namespace Simulturn.Lab.Arena;

/// <summary>
/// Lets bots play against each other and counts who wins: every pairing on every map with both
/// seat orders, so that neither the map nor the seat favours a bot unnoticed.
/// </summary>
public static class ArenaStudy
{
    private const int OpponentSeedOffset = 1_000_000;

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

        MatchResult[] results = new MatchResult[games.Count];
        try
        {
            Parallel.For(0, games.Count, index => results[index] = Play(games[index], options.Limits));
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
                    Wins: row.Count(x => x.Second.WinnerId == x.First.BotPlayerId),
                    Losses: row.Count(x => x.Second.WinnerId == x.First.OpponentPlayerId),
                    Draws: row.Count(x => x.Second.WinnerId is null),
                    AverageTurns: row.Average(x => (double)x.Second.Turns)))
        ];
        return new ArenaResult(options, EngineVersion(), rows);
    }

    private static MatchResult Play(ArenaGame game, MatchLimits limits)
    {
        try
        {
            Dictionary<string, IBot> bots = new()
            {
                { game.BotPlayerId, game.Bot.Create(game.MapSeed) },
                { game.OpponentPlayerId, game.Opponent.Create(game.MapSeed + OpponentSeedOffset) }
            };
            return Match.Play(game.Settings, bots, limits);
        }
        catch (Exception exception)
        {
            throw new ArenaGameException(
                $"{game.Bot.Name} on seat {game.Seat} against {game.Opponent.Name} on map {game.Map} with seed {game.MapSeed}: {exception.Message}",
                exception);
        }
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
