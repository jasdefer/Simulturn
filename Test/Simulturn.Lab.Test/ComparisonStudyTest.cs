using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;
using Simulturn.Lab.Comparison;

namespace Simulturn.Lab.Test;

public sealed class ComparisonStudyTest
{
    /// <summary>
    /// A game of the bot "early" against "late". Only the harvest of the two differs between the games.
    /// </summary>
    private static ArenaGameResult Game(string map, int mapSeed, int seat, ArenaOutcome outcome, MatchEnd end, ushort turns,
        int remainingMatter = 0, int harvested = 0, int opponentHarvested = 0)
    {
        return new ArenaGameResult(
            new ArenaGameKey("early", "late", map, mapSeed, seat),
            outcome,
            end,
            turns,
            remainingMatter,
            new MatterLedger() { Harvested = harvested },
            new MatterLedger() { Harvested = opponentHarvested });
    }

    /// <summary>
    /// On "Open" the bot wins twice from both seats, loses once and draws twice. On "Guarded" it only wins.
    /// </summary>
    private static readonly ImmutableArray<ArenaGameResult> _games =
    [
        Game("Open", 1, 1, ArenaOutcome.Win, MatchEnd.Victory, turns: 10, remainingMatter: 100, harvested: 1000, opponentHarvested: 400),
        Game("Open", 1, 2, ArenaOutcome.Loss, MatchEnd.Victory, turns: 40, remainingMatter: 50, harvested: 300, opponentHarvested: 900),
        Game("Open", 2, 1, ArenaOutcome.Draw, MatchEnd.TurnLimit, turns: 500),
        Game("Open", 2, 2, ArenaOutcome.Win, MatchEnd.Victory, turns: 20, remainingMatter: 300, harvested: 2000, opponentHarvested: 600),
        Game("Open", 3, 1, ArenaOutcome.Draw, MatchEnd.Stall, turns: 100),
        Game("Guarded", 1, 1, ArenaOutcome.Win, MatchEnd.Victory, turns: 30)
    ];

    [Test]
    public void Games_FallIntoAGroupPerMapAndOutcome()
    {
        ImmutableArray<ComparisonGroup> groups = ComparisonStudy.Of(_games);

        groups.Select(x => (x.Bot, x.Opponent, x.Map, x.Outcome, x.Games.Length)).ShouldBe([
            ("early", "late", "Open", ArenaOutcome.Win, 2),
            ("early", "late", "Open", ArenaOutcome.Loss, 1),
            ("early", "late", "Open", ArenaOutcome.Draw, 2),
            ("early", "late", "Guarded", ArenaOutcome.Win, 1)
        ]);
    }

    [Test]
    public void GamesOfBothSeats_FallIntoTheSameGroup()
    {
        ComparisonGroup won = ComparisonStudy.Of(_games)[0];

        won.Games.Select(x => x.Key.Seat).ShouldBe([1, 2]);
    }

    [Test]
    public void Group_AveragesTheGameAndTheEntriesOfBothBots()
    {
        ComparisonGroup won = ComparisonStudy.Of(_games)[0];

        won.AverageTurns.ShouldBe(15);
        won.AverageRemainingMatter.ShouldBe(200);
        won.BotAverage(x => x.Harvested).ShouldBe(1500);
        won.OpponentAverage(x => x.Harvested).ShouldBe(500);
    }

    [Test]
    public void Group_CountsHowItsGamesEnded()
    {
        ComparisonGroup drawn = ComparisonStudy.Of(_games)[2];

        drawn.EndedBy(MatchEnd.TurnLimit).ShouldBe(1);
        drawn.EndedBy(MatchEnd.Stall).ShouldBe(1);
        drawn.EndedBy(MatchEnd.Victory).ShouldBe(0);
    }

    [Test]
    public void NoGames_GiveNoGroup()
    {
        ComparisonStudy.Of([]).ShouldBeEmpty();
    }

    [Test]
    public void Table_ShowsABlockPerMapWithEveryGroupAndEveryEntry()
    {
        string table = ComparisonTable.Render(ComparisonStudy.Of(_games));

        table.ShouldContain("early against late on Open");
        table.ShouldContain("early against late on Guarded");
        table.ShouldContain("Won (2)");
        table.ShouldContain("Lost (1)");
        table.ShouldContain("No winner (2)");
        string[] lines = table.Split('\n', StringSplitOptions.TrimEntries);
        foreach (LedgerEntry entry in LedgerEntries.All)
        {
            lines.Count(x => x.StartsWith(entry.Name + " ", StringComparison.Ordinal)).ShouldBe(2, entry.Name);
        }
        lines.ShouldContain(x => x.StartsWith("Harvested", StringComparison.Ordinal)
            && x.Split(' ', StringSplitOptions.RemoveEmptyEntries).SequenceEqual(new[] { "Harvested", "1500", "500", "300", "900", "0", "0" }));
        lines.ShouldContain(x => x.StartsWith("Ended by Stall", StringComparison.Ordinal));
        lines.ShouldNotContain(x => x.StartsWith("Ended by MutualDefeat", StringComparison.Ordinal));
    }

    [Test]
    public void Csv_HasALinePerGroupAndBot()
    {
        string[] lines = ComparisonTable.RenderCsv(ComparisonStudy.Of(_games)).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        lines.Length.ShouldBe(1 + 2 * 4);
        lines[0].ShouldStartWith("Bot,Opponent,Map,Outcome,Games,Victory,MutualDefeat,Stall,TurnLimit,Turns,RemainingMatter,Player,StartMatter,");
        lines[1].ShouldStartWith("early,late,Open,Win,2,2,0,0,0,15.0,200.0,early,0.0,0.0,1500.0,");
        lines[2].ShouldStartWith("early,late,Open,Win,2,2,0,0,0,15.0,200.0,late,0.0,0.0,500.0,");
        lines.ShouldAllBe(x => x.Split(',').Length == 12 + LedgerEntries.All.Length);
    }
}
