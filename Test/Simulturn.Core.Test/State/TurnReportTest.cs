using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public sealed class TurnReportTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends with 5 dots and a plane each.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02")));

    private static TurnResult FightInTheCenter()
    {
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 5 }),
            ("Player02", _player2Start, _center, new Army() { Dot = 3 })
        ]);
        return new GameState(_gameSettings).ResolveTurn(commands);
    }

    [Test]
    public void Fight_IsReportedOnceWithTheArmiesAndLossesOfAllPlayers()
    {
        TurnResult result = FightInTheCenter();

        FightReport fight = result.Report.Fights.ShouldHaveSingleItem().Value;
        result.Report.Fights.Keys.ShouldBe([_center]);
        fight.Armies.Count.ShouldBe(2);
        fight.Armies["Player01"].ShouldBe(new Army() { Dot = 5 });
        fight.Armies["Player02"].ShouldBe(new Army() { Dot = 3 });
        fight.Losses.Count.ShouldBe(2);
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 3 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 3 });
    }

    [Test]
    public void TurnWithoutFight_HasNoFightInTheReport()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(_noCommands);

        result.Report.Fights.ShouldBeEmpty();
    }

    [Test]
    public void ViewOfResolvedTurn_CarriesTheReportOfThePlayer()
    {
        TurnResult result = FightInTheCenter();

        PlayerGameState view = FogOfWar.GetPlayerGameState(result, "Player02");

        view.Turn.ShouldBe((ushort)1);
        view.Report.Losses.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, Army>(_center, new Army() { Dot = 3 }));
        view.Report.RevealedArmies.ShouldHaveSingleItem().Key.ShouldBe(_center);
        view.Report.RevealedArmies[_center].ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, Army>("Player01", new Army() { Dot = 5 }));
    }

    [Test]
    public void ViewOfStateWithoutResolvedTurn_HasEmptyReport()
    {
        PlayerGameState view = FogOfWar.GetPlayerGameState(new GameState(_gameSettings), "Player01");

        view.Report.Losses.ShouldBeEmpty();
        view.Report.RevealedArmies.ShouldBeEmpty();
    }

    [Test]
    public void ReportDoesNotCarryOverToTheNextTurn()
    {
        TurnResult fight = FightInTheCenter();

        TurnResult next = fight.State.ResolveTurn(_noCommands);

        next.Report.Fights.ShouldBeEmpty();
        FogOfWar.GetPlayerGameState(next, "Player01").Report.RevealedArmies.ShouldBeEmpty();
    }

    [Test]
    public void PlayerReport_HidesFightsThePlayerDidNotTakePartIn()
    {
        TurnReport report = new()
        {
            Fights = ImmutableDictionary<Hexagon, FightReport>.Empty
                .Add(_center, Fight(("Player01", new Army() { Dot = 5 }, new Army() { Dot = 2 }),
                    ("Player02", new Army() { Square = 4 }, new Army() { Square = 1 })))
                .Add(_player2Start, Fight(("Player02", new Army() { Circle = 6 }, new Army() { Circle = 3 }),
                    ("Player03", new Army() { Triangle = 7 }, new Army() { Triangle = 7 })))
        };

        PlayerTurnReport player1 = FogOfWar.GetPlayerTurnReport(report, "Player01");
        PlayerTurnReport player2 = FogOfWar.GetPlayerTurnReport(report, "Player02");

        player1.Losses.Keys.ShouldBe([_center]);
        player1.Losses[_center].ShouldBe(new Army() { Dot = 2 });
        player1.RevealedArmies.Keys.ShouldBe([_center]);
        player1.RevealedArmies[_center].ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, Army>("Player02", new Army() { Square = 4 }));

        player2.Losses.Keys.ShouldBe([_center, _player2Start], ignoreOrder: true);
        player2.RevealedArmies[_center].Keys.ShouldBe(["Player01"]);
        player2.RevealedArmies[_player2Start].Keys.ShouldBe(["Player03"]);
    }

    private static FightReport Fight(params (string PlayerId, Army Army, Army Loss)[] players)
    {
        return new FightReport()
        {
            Armies = players.ToImmutableDictionary(x => x.PlayerId, x => x.Army),
            Losses = players.ToImmutableDictionary(x => x.PlayerId, x => x.Loss)
        };
    }
}
