using System.Text.Json;
using Simulturn.Bots.Catalog;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Lab.Arena;
using Simulturn.Test.Common;

namespace Simulturn.Bots.Test;

public sealed class ArenaStudyTest
{
    private static readonly Army _twentySquares = new() { Square = 20 };

    /// <summary>
    /// A line of 5 hexagons. The players start on (-1,0) and (1,0) with 20 squares each, which
    /// cross to the other start in one turn, and have a free hexagon behind them.
    /// </summary>
    private static readonly GameSettings _lineSettings = TestSettings.Create(Maps.Line(-2, 2)
        .SetItem(new Hexagon(-1, 0), Maps.Start("Player01", matter: 0, army: _twentySquares))
        .SetItem(new Hexagon(1, 0), Maps.Start("Player02", matter: 0, army: _twentySquares)));

    private static readonly ArenaMap _line = new("Line", _ => _lineSettings with { MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 } });

    private static Hexagon StartOf(PlayerGameState view)
    {
        return view.PlayerState.Armies.Keys.Single();
    }

    /// <summary>
    /// Sends all squares into the start of the opponent on the first turn.
    /// </summary>
    private static readonly BotEntry _attacker = new("attacker", _ => ScriptedBot.OnFirstTurn(view =>
        ScriptedBot.Move(StartOf(view), new Hexagon((short)-StartOf(view).X, 0), _twentySquares)));

    /// <summary>
    /// Leaves its start undefended on the first turn and so loses to the attacker.
    /// </summary>
    private static readonly BotEntry _deserter = new("deserter", _ => ScriptedBot.OnFirstTurn(view =>
        ScriptedBot.Move(StartOf(view), new Hexagon((short)(2 * StartOf(view).X), 0), _twentySquares)));

    private static readonly BotEntry _cheater = new("cheater", _ => new ScriptedBot(view =>
        new Dictionary<Hexagon, Command>() { { StartOf(view), new Command() { Training = new Army() { Square = 1 } } } }));

    private static readonly ImmutableArray<BotEntry> _bots = [_attacker, _deserter, _cheater, .. BotCatalog.All];
    private static readonly ImmutableArray<ArenaMap> _maps = [_line];
    private static readonly MatchLimits _limits = new() { StallTurns = 5, MaxTurns = 50 };

    private static ArenaOptions Options(params string[] bots)
    {
        return new ArenaOptions() { Bots = [.. bots], Maps = ["Line"], MapSeeds = 4, Limits = _limits };
    }

    [Test]
    public void StrongerBot_WinsEveryGameFromBothSeats()
    {
        ArenaResult result = ArenaStudy.Run(Options("attacker", "deserter"), _bots, _maps);

        result.Rows.ShouldBe([
            new ArenaRow("attacker", "deserter", "Line", Seat: 1, Wins: 4, Losses: 0, Draws: 0, AverageTurns: 1),
            new ArenaRow("attacker", "deserter", "Line", Seat: 2, Wins: 4, Losses: 0, Draws: 0, AverageTurns: 1)
        ]);
        result.Rows[0].WinRate.ShouldBe(1);
    }

    [Test]
    public void WeakerBot_LosesEveryGameFromBothSeats()
    {
        ArenaResult result = ArenaStudy.Run(Options("deserter", "attacker"), _bots, _maps);

        result.Rows.ShouldBe([
            new ArenaRow("deserter", "attacker", "Line", Seat: 1, Wins: 0, Losses: 4, Draws: 0, AverageTurns: 1),
            new ArenaRow("deserter", "attacker", "Line", Seat: 2, Wins: 0, Losses: 4, Draws: 0, AverageTurns: 1)
        ]);
    }

    [Test]
    public void IdleBots_DrawEveryGameByStall()
    {
        ArenaResult result = ArenaStudy.Run(Options("idle", "idle"), _bots, _maps);

        result.Rows.ShouldBe([
            new ArenaRow("idle", "idle", "Line", Seat: 1, Wins: 0, Losses: 0, Draws: 4, AverageTurns: 5),
            new ArenaRow("idle", "idle", "Line", Seat: 2, Wins: 0, Losses: 0, Draws: 4, AverageTurns: 5)
        ]);
    }

    [Test]
    public void ThreeBots_PlayEveryPairingOnEverySeat()
    {
        ArenaResult result = ArenaStudy.Run(Options("attacker", "deserter", "idle"), _bots, _maps);

        result.Rows.Select(x => (x.Bot, x.Opponent, x.Seat)).ShouldBe([
            ("attacker", "deserter", 1), ("attacker", "deserter", 2),
            ("attacker", "idle", 1), ("attacker", "idle", 2),
            ("deserter", "idle", 1), ("deserter", "idle", 2)
        ]);
        result.Rows.ShouldAllBe(x => x.Games == 4);
    }

    [Test]
    public void SameOptions_GiveTheSameResult()
    {
        ArenaOptions options = Options("attacker", "deserter", "idle");

        string first = JsonSerializer.Serialize(ArenaStudy.Run(options, _bots, _maps));
        string second = JsonSerializer.Serialize(ArenaStudy.Run(options, _bots, _maps));

        second.ShouldBe(first);
    }

    [Test]
    public void BotWithInvalidCommands_FailsTheRunAndNamesTheGame()
    {
        var exception = Should.Throw<ArenaGameException>(() => ArenaStudy.Run(Options("idle", "cheater"), _bots, _maps));

        exception.InnerException.ShouldBeOfType<InvalidBotCommandsException>();
        exception.Message.ShouldContain("idle on seat");
        exception.Message.ShouldContain("against cheater on map Line with seed");
    }

    [Test]
    public void UnknownBot_Throws()
    {
        Should.Throw<ArgumentException>(() => ArenaStudy.Run(Options("attacker", "nobody"), _bots, _maps));
    }

    [Test]
    public void UnknownMap_Throws()
    {
        ArenaOptions options = Options("attacker", "deserter") with { Maps = ["Nowhere"] };

        Should.Throw<ArgumentException>(() => ArenaStudy.Run(options, _bots, _maps));
    }

    [Test]
    public void SingleBot_Throws()
    {
        Should.Throw<ArgumentException>(() => ArenaStudy.Run(Options("attacker"), _bots, _maps));
    }

    [Test]
    public void BotsOfTheCatalog_PlayOnEveryMapOfTheArena()
    {
        ArenaOptions options = new()
        {
            Bots = [.. BotCatalog.All.Select(x => x.Name), BotCatalog.All[0].Name],
            Maps = [.. ArenaMaps.All.Select(x => x.Name)],
            MapSeeds = 2,
            Limits = new MatchLimits() { StallTurns = 5, MaxTurns = 60 }
        };

        ArenaResult result = ArenaStudy.Run(options, BotCatalog.All, ArenaMaps.All);

        result.Rows.Select(x => x.Map).Distinct().ShouldBe(["Open", "HomeOnly", "Expansions", "Guarded"]);
        result.Rows.ShouldAllBe(x => x.Games == 2);
        result.EngineVersion.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Table_ShowsEveryRow()
    {
        ArenaResult result = ArenaStudy.Run(Options("attacker", "deserter"), _bots, _maps);

        string table = ArenaTable.Render(result);

        table.ShouldContain("attacker against deserter");
        table.Split('\n').Count(x => x.TrimStart().StartsWith("Line", StringComparison.Ordinal)).ShouldBe(2);
    }
}
