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
    /// A map of 3 hexagons in a line. The players start on the opposite ends with 5 dots and a plane each
    /// and 1000 matter on their start hexagon. Player01 can research the dot upgrade at home.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01").WithResearch(Upgrade.DotUpgrade))
        .SetItem(_player2Start, Maps.Start("Player02")));

    /// <summary>
    /// The same map for a raid: Player01 also owns a cube, Player02 starts with 10 squares.
    /// </summary>
    private static readonly GameSettings _raidSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01", compound: new Compound() { Plane = 1, Cube = 1 }).WithResearch(Upgrade.DotUpgrade))
        .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 })));

    private static Dictionary<string, Dictionary<Hexagon, Command>> Player1Orders(Command command)
    {
        return DictionaryExtensions.CommandsToDictionary([("Player01", _player1Start, command)]);
    }

    private static TurnResult FightInTheCenter()
    {
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 5 }),
            ("Player02", _player2Start, _center, new Army() { Dot = 3 })
        ]);
        return new GameState(_gameSettings).ResolveTurn(commands);
    }

    /// <summary>
    /// Player02 marches 10 squares into the home of Player01, who in the same turn orders a square,
    /// an axis and the dot upgrade there. The 5 dots die, the 6 surviving squares destroy the cube.
    /// </summary>
    private static TurnResult Raid()
    {
        GameState turn1 = new GameState(_raidSettings).PlayTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player2Start, _center, new Army() { Square = 10 })
        ]));
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command()
            {
                Training = new Army() { Square = 1 },
                Construction = new Compound() { Axis = 1 },
                Research = Upgrade.DotUpgrade
            }),
            ("Player02", _center, new Command()
            {
                MovementCommands = [new MovementCommand() { Destination = _player1Start, Army = new Army() { Square = 10 } }]
            })
        ]);
        return turn1.ResolveTurn(commands);
    }

    [Test]
    public void Income_IsReportedPerHexagonWithTheHarvesters()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(_noCommands);

        PlayerActivityReport activity = result.Report.Activities["Player01"];
        activity.Incomes.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, IncomeReport>(
            _player1Start, new IncomeReport(new Army() { Dot = 5 }, 50)));
        activity.MatterBefore.ShouldBe(500);
        activity.MatterAfter.ShouldBe(550);
    }

    [Test]
    public void Matter_IsReportedBeforeAndAfterIncomeAndOrders()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(Player1Orders(new Command() { Training = new Army() { Dot = 1 } }));

        PlayerActivityReport activity = result.Report.Activities["Player01"];
        activity.MatterBefore.ShouldBe(500);
        activity.MatterAfter.ShouldBe(475);
        activity.MatterAfter.ShouldBe(result.State.PlayerStates["Player01"].Matter);
    }

    [Test]
    public void TrainingWithDurationOne_IsReportedAsCompletedInTheTurnItIsOrdered()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(Player1Orders(new Command() { Training = new Army() { Dot = 1 } }));

        result.Report.Activities["Player01"].CompletedTrainings.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Army>(_player1Start, new Army() { Dot = 1 }));
        result.State.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
    }

    [Test]
    public void Construction_IsReportedInTheTurnItCompletes()
    {
        TurnResult ordered = new GameState(_gameSettings).ResolveTurn(Player1Orders(new Command() { Construction = new Compound() { Axis = 1 } }));
        TurnResult completed = ordered.State.ResolveTurn(_noCommands);

        ordered.Report.Activities["Player01"].CompletedConstructions.ShouldBeEmpty();
        completed.Report.Activities["Player01"].CompletedConstructions.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Compound>(_player1Start, new Compound() { Axis = 1 }));
    }

    [Test]
    public void Research_IsReportedWithTheReachedLevelInTheTurnItCompletes()
    {
        TurnResult ordered = new GameState(_gameSettings).ResolveTurn(Player1Orders(new Command() { Research = Upgrade.DotUpgrade }));
        TurnResult waiting = ordered.State.ResolveTurn(_noCommands);
        TurnResult completed = waiting.State.ResolveTurn(_noCommands);

        ordered.Report.Activities["Player01"].CompletedResearches.ShouldBeEmpty();
        waiting.Report.Activities["Player01"].CompletedResearches.ShouldBeEmpty();
        completed.Report.Activities["Player01"].CompletedResearches.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, ResearchReport>(_player1Start, new ResearchReport(Upgrade.DotUpgrade, 1)));
    }

    [Test]
    public void Movement_IsReportedWithOriginDestinationAndArmy()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 2 })
        ]));

        result.Report.Activities["Player01"].Movements.ShouldHaveSingleItem()
            .ShouldBe(new MovementReport(_player1Start, _center, new Army() { Dot = 2 }));
        result.Report.Activities["Player02"].Movements.ShouldBeEmpty();
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
    public void FightWithoutOwnLosses_IsReportedToBothPlayers()
    {
        // Arrange: circles counter squares, so 3 squares have no strength against 5 circles.
        GameSettings settings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01", army: new Army() { Circle = 5 }))
            .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 3 })));
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Circle = 5 }),
            ("Player02", _player2Start, _center, new Army() { Square = 3 })
        ]);

        // Act
        TurnResult result = new GameState(settings).ResolveTurn(commands);

        // Assert
        FightReport fight = result.Report.Fights.ShouldHaveSingleItem().Value;
        fight.Armies["Player01"].ShouldBe(new Army() { Circle = 5 });
        fight.Losses["Player01"].ShouldBe(Army.Empty);
        fight.Losses["Player02"].ShouldBe(new Army() { Square = 3 });
        FogOfWar.GetPlayerTurnReport(result.Report, "Player01").Fights[_center].ShouldBe(fight);
        FogOfWar.GetPlayerTurnReport(result.Report, "Player02").Fights[_center].ShouldBe(fight);
    }

    [Test]
    public void DestroyedBuildings_AreReportedWithAttackerAndOwner()
    {
        TurnResult result = Raid();

        DestructionReport destruction = result.Report.Destructions.ShouldHaveSingleItem();
        destruction.ShouldBe(new DestructionReport(_player1Start, "Player02", "Player01", new Compound() { Cube = 1 }));
        FogOfWar.GetPlayerTurnReport(result.Report, "Player01").Destructions.ShouldHaveSingleItem().ShouldBe(destruction);
        FogOfWar.GetPlayerTurnReport(result.Report, "Player02").Destructions.ShouldHaveSingleItem().ShouldBe(destruction);
    }

    [Test]
    public void CanceledOrders_AreReportedToTheirOwner()
    {
        TurnResult result = Raid();

        PlayerActivityReport activity = result.Report.Activities["Player01"];
        activity.CanceledConstructions.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Compound>(_player1Start, new Compound() { Axis = 1 }));
        activity.CanceledTrainings.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Army>(_player1Start, new Army() { Square = 1 }));
        activity.CanceledResearches.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Upgrade>(_player1Start, Upgrade.DotUpgrade));
        PlayerState player = result.State.PlayerStates["Player01"];
        player.Constructions.ShouldBeEmpty();
        player.Researches.ShouldBeEmpty();
        player.UsedSpace.ShouldBe(0);
    }

    [Test]
    public void TurnWithoutFight_HasNoFightInTheReport()
    {
        TurnResult result = new GameState(_gameSettings).ResolveTurn(_noCommands);

        result.Report.Fights.ShouldBeEmpty();
        result.Report.Destructions.ShouldBeEmpty();
    }

    [Test]
    public void ViewOfResolvedTurn_CarriesTheReportOfThePlayer()
    {
        TurnResult result = FightInTheCenter();

        PlayerGameState view = FogOfWar.GetPlayerGameState(result, "Player02");

        view.Turn.ShouldBe((ushort)1);
        view.Report.Activity.ShouldBe(result.Report.Activities["Player02"]);
        view.Report.Fights.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, FightReport>(_center, result.Report.Fights[_center]));
    }

    [Test]
    public void ViewOfStateWithoutResolvedTurn_HasEmptyReport()
    {
        PlayerGameState view = FogOfWar.GetPlayerGameState(new GameState(_gameSettings), "Player01");

        view.Report.Activity.ShouldBe(PlayerActivityReport.Empty);
        view.Report.Fights.ShouldBeEmpty();
        view.Report.Destructions.ShouldBeEmpty();
    }

    [Test]
    public void ReportDoesNotCarryOverToTheNextTurn()
    {
        TurnResult fight = FightInTheCenter();

        TurnResult next = fight.State.ResolveTurn(_noCommands);

        next.Report.Fights.ShouldBeEmpty();
        next.Report.Activities["Player01"].Movements.ShouldBeEmpty();
        FogOfWar.GetPlayerGameState(next, "Player01").Report.Fights.ShouldBeEmpty();
    }

    [Test]
    public void PlayerReport_HidesTheActivityOfOtherPlayers()
    {
        TurnResult result = Raid();

        PlayerTurnReport player2 = FogOfWar.GetPlayerTurnReport(result.Report, "Player02");

        player2.Activity.ShouldBe(result.Report.Activities["Player02"]);
        player2.Activity.Movements.ShouldHaveSingleItem().Origin.ShouldBe(_center);
        player2.Activity.Incomes.ShouldBeEmpty();
        player2.Activity.CanceledTrainings.ShouldBeEmpty();
        player2.Activity.CanceledConstructions.ShouldBeEmpty();
        player2.Activity.CanceledResearches.ShouldBeEmpty();
    }

    [Test]
    public void PlayerReport_HidesFightsAndDestructionsThePlayerDidNotTakePartIn()
    {
        FightReport centerFight = Fight(("Player01", new Army() { Dot = 5 }, new Army() { Dot = 2 }),
            ("Player02", new Army() { Square = 4 }, new Army() { Square = 1 }));
        FightReport homeFight = Fight(("Player02", new Army() { Circle = 6 }, new Army() { Circle = 3 }),
            ("Player03", new Army() { Triangle = 7 }, new Army() { Triangle = 7 }));
        DestructionReport destruction = new(_player2Start, "Player03", "Player02", new Compound() { Plane = 1 });
        TurnReport report = new()
        {
            Fights = ImmutableDictionary<Hexagon, FightReport>.Empty
                .Add(_center, centerFight)
                .Add(_player2Start, homeFight),
            Destructions = [destruction]
        };

        PlayerTurnReport player1 = FogOfWar.GetPlayerTurnReport(report, "Player01");
        PlayerTurnReport player2 = FogOfWar.GetPlayerTurnReport(report, "Player02");
        PlayerTurnReport player3 = FogOfWar.GetPlayerTurnReport(report, "Player03");

        player1.Fights.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, FightReport>(_center, centerFight));
        player1.Destructions.ShouldBeEmpty();
        player2.Fights.Keys.ShouldBe([_center, _player2Start], ignoreOrder: true);
        player2.Destructions.ShouldHaveSingleItem().ShouldBe(destruction);
        player3.Fights.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, FightReport>(_player2Start, homeFight));
        player3.Destructions.ShouldHaveSingleItem().ShouldBe(destruction);
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
