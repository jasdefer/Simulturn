using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Comparison;

/// <summary>
/// Compares the games of an arena run: what the ledgers of the games one bot won, the other won
/// and nobody won look like on average, per pairing and map type. It plays nothing, it only
/// reads the games of a run.
/// </summary>
public static class ComparisonStudy
{
    /// <summary>
    /// The groups of the games: per pairing and map type in the order of the games, each with
    /// the games won by the first bot, by the bots after it and by nobody. Both seats fall into
    /// the same group. A winner without a game has no group.
    /// </summary>
    public static ImmutableArray<ComparisonGroup> Of(IReadOnlyList<ArenaGameResult> games)
    {
        return
        [
            .. games
                .GroupBy(x => (x.Key.Bots, x.Key.Map))
                .SelectMany(pairing => pairing.Key.Bots.Places
                    .Select(bot => (int?)bot)
                    .Append(null)
                    .Select(winner => new ComparisonGroup(
                        pairing.Key.Bots,
                        pairing.Key.Map,
                        winner,
                        [.. pairing.Where(x => x.Winner == winner)]))
                    .Where(x => !x.Games.IsEmpty))
        ];
    }
}

/// <summary>
/// The games of one pairing on one map type that the same bot won, or that nobody won.
/// </summary>
/// <param name="Bots">The bots of the pairing in its order.</param>
/// <param name="Map">The map type.</param>
/// <param name="Winner">The place of the bot that won the games among the bots, or null when nobody won them.</param>
/// <param name="Games">The games of the group, at least one.</param>
public sealed record ComparisonGroup(PerBot<string> Bots, string Map, int? Winner, ImmutableArray<ArenaGameResult> Games)
{
    /// <summary>The name of the bot that won the games, or null when nobody won them.</summary>
    public string? WinnerName => Winner is { } bot ? Bots[bot] : null;

    /// <summary>The average number of turns of a game.</summary>
    public double AverageTurns => Games.Average(x => (double)x.Turns);

    /// <summary>The average matter left on the map at the end of a game.</summary>
    public double AverageRemainingMatter => Games.Average(x => (double)x.RemainingMatter);

    /// <summary>
    /// The number of games that ended the given way.
    /// </summary>
    public int EndedBy(MatchEnd end)
    {
        return Games.Count(x => x.End == end);
    }

    /// <summary>
    /// The average of an entry over the ledgers of the bot at the given place.
    /// </summary>
    public double Average(int bot, Func<MatterLedger, int> entry)
    {
        return Games.Average(x => (double)entry(x.Ledgers[bot]));
    }

    /// <summary>
    /// The game to look at in place of the group: the one whose ledgers lie closest to the
    /// averages of the group. The distance of a game is the matter its entries are away from
    /// their averages, added up over every entry of the ledgers of all bots. Every entry is
    /// matter, so nothing is weighted. Of two games equally close, the first of the run is taken.
    /// </summary>
    public ArenaGameResult Typical()
    {
        var averages = LedgerEntries.All
            .Select(x => (x.Value, PerBot: Bots.Places.Select(bot => Average(bot, x.Value)).ToList()))
            .ToList();
        return Games.MinBy(game => averages.Sum(x =>
            Bots.Places.Sum(bot => Math.Abs(x.Value(game.Ledgers[bot]) - x.PerBot[bot]))))!;
    }

    /// <summary>
    /// The games with the lowest and the highest value of an entry in the ledger of the bot at
    /// the given place, or null when the entry is the same in every game of the group.
    /// </summary>
    public ComparisonOutliers? Outliers(int bot, Func<MatterLedger, int> entry)
    {
        ArenaGameResult lowest = Games.MinBy(x => entry(x.Ledgers[bot]))!;
        ArenaGameResult highest = Games.MaxBy(x => entry(x.Ledgers[bot]))!;
        return entry(lowest.Ledgers[bot]) == entry(highest.Ledgers[bot]) ? null : new ComparisonOutliers(lowest, highest);
    }
}

/// <summary>
/// The two games of a group that lie at the ends of one entry. Of several games with the same
/// value, the first of the run is taken.
/// </summary>
/// <param name="Lowest">The game with the lowest value of the entry.</param>
/// <param name="Highest">The game with the highest value of the entry.</param>
public sealed record ComparisonOutliers(ArenaGameResult Lowest, ArenaGameResult Highest);
