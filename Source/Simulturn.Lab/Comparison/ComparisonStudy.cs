using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Comparison;

/// <summary>
/// Compares the games of an arena run: what the ledgers of the games a bot won, lost and drew
/// against one opponent on one map type look like on average. It plays nothing, it only reads
/// the games of a run.
/// </summary>
public static class ComparisonStudy
{
    private static readonly ImmutableArray<ArenaOutcome> _outcomes = [ArenaOutcome.Win, ArenaOutcome.Loss, ArenaOutcome.Draw];

    /// <summary>
    /// The groups of the games: per bot, opponent and map type in the order of the games, each
    /// with the games won, lost and drawn by the bot. Both seats fall into the same group. An
    /// outcome without a game has no group.
    /// </summary>
    public static ImmutableArray<ComparisonGroup> Of(IReadOnlyList<ArenaGameResult> games)
    {
        return
        [
            .. games
                .GroupBy(x => (x.Key.Bot, x.Key.Opponent, x.Key.Map))
                .SelectMany(pairing => _outcomes
                    .Select(outcome => new ComparisonGroup(
                        pairing.Key.Bot,
                        pairing.Key.Opponent,
                        pairing.Key.Map,
                        outcome,
                        [.. pairing.Where(x => x.Outcome == outcome)]))
                    .Where(x => !x.Games.IsEmpty))
        ];
    }
}

/// <summary>
/// The games of one bot against one opponent on one map type that ended the same way for the bot.
/// </summary>
/// <param name="Bot">The bot the outcome is seen from.</param>
/// <param name="Opponent">The bot it played against.</param>
/// <param name="Map">The map type.</param>
/// <param name="Outcome">How the games ended for the bot.</param>
/// <param name="Games">The games of the group, at least one.</param>
public sealed record ComparisonGroup(string Bot, string Opponent, string Map, ArenaOutcome Outcome, ImmutableArray<ArenaGameResult> Games)
{
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
    /// The average of an entry over the ledgers of the bot.
    /// </summary>
    public double BotAverage(Func<MatterLedger, int> entry)
    {
        return Games.Average(x => (double)entry(x.Bot));
    }

    /// <summary>
    /// The average of an entry over the ledgers of the opponent.
    /// </summary>
    public double OpponentAverage(Func<MatterLedger, int> entry)
    {
        return Games.Average(x => (double)entry(x.Opponent));
    }

    /// <summary>
    /// The game to look at in place of the group: the one whose ledgers lie closest to the
    /// averages of the group. The distance of a game is the matter its entries are away from
    /// their averages, added up over every entry of both ledgers. Every entry is matter, so
    /// nothing is weighted. Of two games equally close, the first of the run is taken.
    /// </summary>
    public ArenaGameResult Typical()
    {
        var averages = LedgerEntries.All
            .Select(x => (x.Value, OfBot: BotAverage(x.Value), OfOpponent: OpponentAverage(x.Value)))
            .ToList();
        return Games.MinBy(game => averages.Sum(x =>
            Math.Abs(x.Value(game.Bot) - x.OfBot) + Math.Abs(x.Value(game.Opponent) - x.OfOpponent)))!;
    }

    /// <summary>
    /// The games with the lowest and the highest value of an entry in the ledger of the bot, or
    /// null when the entry is the same in every game of the group.
    /// </summary>
    public ComparisonOutliers? BotOutliers(Func<MatterLedger, int> entry)
    {
        return Outliers(x => entry(x.Bot));
    }

    /// <summary>
    /// The games with the lowest and the highest value of an entry in the ledger of the opponent,
    /// or null when the entry is the same in every game of the group.
    /// </summary>
    public ComparisonOutliers? OpponentOutliers(Func<MatterLedger, int> entry)
    {
        return Outliers(x => entry(x.Opponent));
    }

    private ComparisonOutliers? Outliers(Func<ArenaGameResult, int> value)
    {
        ArenaGameResult lowest = Games.MinBy(value)!;
        ArenaGameResult highest = Games.MaxBy(value)!;
        return value(lowest) == value(highest) ? null : new ComparisonOutliers(lowest, highest);
    }
}

/// <summary>
/// The two games of a group that lie at the ends of one entry. Of several games with the same
/// value, the first of the run is taken.
/// </summary>
/// <param name="Lowest">The game with the lowest value of the entry.</param>
/// <param name="Highest">The game with the highest value of the entry.</param>
public sealed record ComparisonOutliers(ArenaGameResult Lowest, ArenaGameResult Highest);
