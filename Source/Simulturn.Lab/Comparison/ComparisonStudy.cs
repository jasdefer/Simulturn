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
}
