using Simulturn.Bots.Catalog;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Bots.Test;

public sealed class MatchTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Army _fiveDots = new() { Dot = 5 };
    private static readonly Army _twentySquares = new() { Square = 20 };
    private static readonly MatchLimits _limits = new();

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends, and squares
    /// cross the whole line in one turn.
    /// </summary>
    private static GameSettings CreateSettings(Army player1Army, Army player2Army, int matter = 0)
    {
        GameSettings line = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01", matter, player1Army))
            .SetItem(_player2Start, Maps.Start("Player02", matter, player2Army)));
        return line with { MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 } };
    }

    private static Dictionary<string, IBot> Bots(IBot player1, IBot player2)
    {
        return new Dictionary<string, IBot>() { { "Player01", player1 }, { "Player02", player2 } };
    }

    [Test]
    public void AttackOnADefencelessStart_EndsInVictory()
    {
        GameSettings settings = CreateSettings(_twentySquares, _fiveDots);
        var attacker = ScriptedBot.OnFirstTurn(_ => ScriptedBot.Move(_player1Start, _player2Start, _twentySquares));

        MatchResult result = Match.Play(settings, Bots(attacker, new IdleBot()), _limits);

        result.ShouldBe(MatchResult.WonBy("Player01", 1));
    }

    [Test]
    public void SimultaneousBaseTrade_EndsInMutualDefeat()
    {
        GameSettings settings = CreateSettings(_twentySquares, _twentySquares);
        var player1 = ScriptedBot.OnFirstTurn(_ => ScriptedBot.Move(_player1Start, _player2Start, _twentySquares));
        var player2 = ScriptedBot.OnFirstTurn(_ => ScriptedBot.Move(_player2Start, _player1Start, _twentySquares));

        MatchResult result = Match.Play(settings, Bots(player1, player2), _limits);

        result.ShouldBe(MatchResult.Drawn(MatchEnd.MutualDefeat, 1));
    }

    [Test]
    public void IdleBotsWithoutIncome_EndInStall()
    {
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots);

        MatchResult result = Match.Play(settings, Bots(new IdleBot(), new IdleBot()), new MatchLimits() { StallTurns = 10, MaxTurns = 100 });

        result.ShouldBe(MatchResult.Drawn(MatchEnd.Stall, 10));
    }

    [Test]
    public void IdleBotsWithIncome_AreNotStalledAndEndAtTheTurnLimit()
    {
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots, matter: 1000);

        MatchResult result = Match.Play(settings, Bots(new IdleBot(), new IdleBot()), new MatchLimits() { StallTurns = 3, MaxTurns = 5 });

        result.ShouldBe(MatchResult.Drawn(MatchEnd.TurnLimit, 5));
    }

    [Test]
    public void Activity_RestartsTheStallCount()
    {
        // A dot walks to the center on every third turn, so there are never 3 still turns in a row.
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots);
        var walker = new ScriptedBot(view => view.Turn % 3 == 0
            ? ScriptedBot.Move(_player1Start, _center, new Army() { Dot = 1 })
            : ScriptedBot.NoCommands);

        MatchResult result = Match.Play(settings, Bots(walker, new IdleBot()), new MatchLimits() { StallTurns = 3, MaxTurns = 12 });

        result.ShouldBe(MatchResult.Drawn(MatchEnd.TurnLimit, 12));
    }

    [Test]
    public void EveryBot_GetsTheViewOfItsPlayerOncePerTurn()
    {
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots);
        List<(string PlayerId, ushort Turn)> player1Views = [];
        List<(string PlayerId, ushort Turn)> player2Views = [];
        var player1 = new ScriptedBot(view => { player1Views.Add((view.PlayerId, view.Turn)); return ScriptedBot.NoCommands; });
        var player2 = new ScriptedBot(view => { player2Views.Add((view.PlayerId, view.Turn)); return ScriptedBot.NoCommands; });

        Match.Play(settings, Bots(player1, player2), new MatchLimits() { MaxTurns = 3 });

        player1Views.ShouldBe([("Player01", 0), ("Player01", 1), ("Player01", 2)]);
        player2Views.ShouldBe([("Player02", 0), ("Player02", 1), ("Player02", 2)]);
    }

    [Test]
    public void Observer_GetsTheStartAndTheResultOfEveryTurn()
    {
        GameSettings settings = CreateSettings(_twentySquares, _fiveDots);
        var attacker = ScriptedBot.OnFirstTurn(_ => ScriptedBot.Move(_player1Start, _player2Start, _twentySquares));
        List<TurnResult> turns = [];

        MatchResult result = Match.Play(settings, Bots(attacker, new IdleBot()), _limits, turns.Add);

        turns.Select(x => x.State.Turn).ShouldBe([(ushort)0, (ushort)1]);
        turns[0].Report.ShouldBe(TurnReport.Empty);
        turns[1].Report.Defeats.ShouldBe(["Player02"]);
        turns[1].State.Turn.ShouldBe(result.Turns);
    }

    [Test]
    public void InvalidCommands_ThrowWithThePlayerAndTheTurn()
    {
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots);
        // Training a square requires a cube, which the player does not have.
        var cheater = new ScriptedBot(view => view.Turn == 2
            ? new Dictionary<Hexagon, Command>() { { _player2Start, new Command() { Training = new Army() { Square = 1 } } } }
            : ScriptedBot.NoCommands);

        var exception = Should.Throw<InvalidBotCommandsException>(() => Match.Play(settings, Bots(new IdleBot(), cheater), _limits));

        exception.PlayerId.ShouldBe("Player02");
        exception.Turn.ShouldBe((ushort)2);
        exception.Validations.ShouldNotBeEmpty();
    }

    [Test]
    public void BotsThatAreNotThePlayersOfTheSettings_Throw()
    {
        GameSettings settings = CreateSettings(_fiveDots, _fiveDots);
        var bots = new Dictionary<string, IBot>() { { "Player01", new IdleBot() } };

        Should.Throw<ArgumentException>(() => Match.Play(settings, bots, _limits));
    }
}
