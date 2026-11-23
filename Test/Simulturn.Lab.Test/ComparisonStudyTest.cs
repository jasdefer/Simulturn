using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;
using Simulturn.Lab.Comparison;

namespace Simulturn.Lab.Test;

public sealed class ComparisonStudyTest
{
    private static readonly PerBot<string> _bots = ["early", "late"];

    /// <summary>
    /// A game of "early" against "late". Only the harvest of the two differs between the games.
    /// </summary>
    private static ArenaGameResult Game(string map, int mapSeed, int seat, int? winner, MatchEnd end, ushort turns,
        int remainingMatter = 0, int firstHarvested = 0, int secondHarvested = 0)
    {
        return new ArenaGameResult(
            new ArenaGameKey(_bots, map, mapSeed, seat),
            winner,
            end,
            turns,
            remainingMatter,
            [new MatterLedger() { Harvested = firstHarvested }, new MatterLedger() { Harvested = secondHarvested }]);
    }

    /// <summary>
    /// On "Open" the first bot wins twice from both seats, the second once and nobody twice. On "Guarded" only the first wins.
    /// </summary>
    private static readonly ImmutableArray<ArenaGameResult> _games =
    [
        Game("Open", 1, 1, 0, MatchEnd.Victory, turns: 10, remainingMatter: 100, firstHarvested: 1000, secondHarvested: 400),
        Game("Open", 1, 2, 1, MatchEnd.Victory, turns: 40, remainingMatter: 50, firstHarvested: 300, secondHarvested: 900),
        Game("Open", 2, 1, null, MatchEnd.TurnLimit, turns: 500),
        Game("Open", 2, 2, 0, MatchEnd.Victory, turns: 20, remainingMatter: 300, firstHarvested: 2000, secondHarvested: 600),
        Game("Open", 3, 1, null, MatchEnd.Stall, turns: 100),
        Game("Guarded", 1, 1, 0, MatchEnd.Victory, turns: 30)
    ];

    [Test]
    public void Games_FallIntoAGroupPerMapAndWinner()
    {
        ImmutableArray<ComparisonGroup> groups = ComparisonStudy.Of(_games);

        groups.Select(x => (x.Bots, x.Map, x.Winner, x.Games.Length)).ShouldBe([
            (_bots, "Open", (int?)0, 2),
            (_bots, "Open", 1, 1),
            (_bots, "Open", null, 2),
            (_bots, "Guarded", 0, 1)
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
        won.Average(0, x => x.Harvested).ShouldBe(1500);
        won.Average(1, x => x.Harvested).ShouldBe(500);
    }

    [Test]
    public void Group_CountsHowItsGamesEnded()
    {
        ComparisonGroup drawn = ComparisonStudy.Of(_games)[2];

        drawn.EndedBy(MatchEnd.TurnLimit).ShouldBe(1);
        drawn.EndedBy(MatchEnd.Stall).ShouldBe(1);
        drawn.EndedBy(MatchEnd.Victory).ShouldBe(0);
    }

    /// <summary>
    /// Three games on one map won by the first bot: it harvests 100, 400 and 1000, the second always 200.
    /// The averages are 500 and 200, so the second game is the closest.
    /// </summary>
    private static readonly ImmutableArray<ArenaGameResult> _spread =
    [
        Game("Open", 1, 1, 0, MatchEnd.Victory, turns: 10, firstHarvested: 100, secondHarvested: 200),
        Game("Open", 1, 2, 0, MatchEnd.Victory, turns: 10, firstHarvested: 400, secondHarvested: 200),
        Game("Open", 2, 1, 0, MatchEnd.Victory, turns: 10, firstHarvested: 1000, secondHarvested: 200)
    ];

    [Test]
    public void TypicalGame_IsTheOneClosestToTheAveragesOfTheGroup()
    {
        ComparisonGroup won = ComparisonStudy.Of(_spread)[0];

        won.Typical().Key.ShouldBe(new ArenaGameKey(["early", "late"], "Open", 1, 2));
    }

    [Test]
    public void TypicalGame_CountsTheLedgersOfBothBots()
    {
        ComparisonGroup won = ComparisonStudy.Of(
        [
            Game("Open", 1, 1, 0, MatchEnd.Victory, turns: 10, firstHarvested: 500, secondHarvested: 0),
            Game("Open", 1, 2, 0, MatchEnd.Victory, turns: 10, firstHarvested: 500, secondHarvested: 400),
            Game("Open", 2, 1, 0, MatchEnd.Victory, turns: 10, firstHarvested: 500, secondHarvested: 1100)
        ])[0];

        won.Typical().Key.MapSeed.ShouldBe(1);
        won.Typical().Key.Seat.ShouldBe(2);
    }

    [Test]
    public void TypicalGame_OfGamesEquallyClose_IsTheFirstOfTheRun()
    {
        ComparisonGroup drawn = ComparisonStudy.Of(_games)[2];

        drawn.Typical().Key.MapSeed.ShouldBe(2);
    }

    [Test]
    public void Outliers_AreTheGamesWithTheLowestAndTheHighestValueOfAnEntry()
    {
        ComparisonGroup won = ComparisonStudy.Of(_spread)[0];

        ComparisonOutliers outliers = won.Outliers(0, x => x.Harvested).ShouldNotBeNull();

        outliers.Lowest.Key.ShouldBe(new ArenaGameKey(["early", "late"], "Open", 1, 1));
        outliers.Highest.Key.ShouldBe(new ArenaGameKey(["early", "late"], "Open", 2, 1));
    }

    [Test]
    public void EntryThatIsTheSameInEveryGame_HasNoOutliers()
    {
        ComparisonGroup won = ComparisonStudy.Of(_spread)[0];

        won.Outliers(1, x => x.Harvested).ShouldBeNull();
        won.Outliers(0, x => x.Fallen).ShouldBeNull();
    }

    [Test]
    public void PicksCsv_NamesTheTypicalGameAndTheOutliersOfEveryGroup()
    {
        string[] lines = ComparisonTable.RenderPicksCsv(ComparisonStudy.Of(_spread)).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        lines.ShouldBe([
            "FirstBot,SecondBot,Map,Winner,Games,Pick,Bot,Entry,Value,Average,MapSeed,Seat",
            "early,late,Open,early,3,Typical,,,,,1,2",
            "early,late,Open,early,3,Lowest,early,Harvested,100,500.0,1,1",
            "early,late,Open,early,3,Highest,early,Harvested,1000,500.0,2,1"
        ]);
    }

    [Test]
    public void Table_NamesTheTypicalGameOfEveryGroup()
    {
        string[] lines = ComparisonTable.Render(ComparisonStudy.Of(_games)).Split('\n', StringSplitOptions.TrimEntries);

        lines.ShouldContain(x => x.StartsWith("Typical game", StringComparison.Ordinal)
            && x.Split(' ', StringSplitOptions.RemoveEmptyEntries).SequenceEqual(
                new[] { "Typical", "game", "seed", "1", "seat", "1", "seed", "1", "seat", "2", "seed", "2", "seat", "1" }));
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
        table.ShouldContain("early won (2)");
        table.ShouldContain("late won (1)");
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
        lines[0].ShouldStartWith("FirstBot,SecondBot,Map,Winner,Games,Victory,MutualDefeat,Stall,TurnLimit,Turns,RemainingMatter,Bot,StartMatter,");
        lines[1].ShouldStartWith("early,late,Open,early,2,2,0,0,0,15.0,200.0,early,0.0,0.0,1500.0,");
        lines[2].ShouldStartWith("early,late,Open,early,2,2,0,0,0,15.0,200.0,late,0.0,0.0,500.0,");
        lines.ShouldAllBe(x => x.Split(',').Length == 12 + LedgerEntries.All.Length);
    }
}
