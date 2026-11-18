using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

public sealed class DefeatTest
{
    private static readonly Hexagon _outpost = new Hexagon(-2, 0);
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Dictionary<Hexagon, Command> _noOrders = [];

    /// <summary>
    /// A map of 4 hexagons in a line. Player01 starts on (-1,0) with 5 dots and a plane and has an
    /// outpost hexagon behind it that offers the triangle upgrade. Player02 starts on (1,0) with 10 squares.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-2, 1)
        .SetItem(_outpost, Maps.Hexagon().WithResearch(Upgrade.TriangleExponent))
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 })));

    private static Command Move(Hexagon destination, Army army)
    {
        return new Command() { Movements = [new MovementOrder() { Destination = destination, Army = army }] };
    }

    /// <summary>
    /// Player01 sends 2 dots to the outpost while the 10 squares of Player02 reach the center.
    /// </summary>
    private static GameState SquaresInTheCenter()
    {
        return new GameState(_gameSettings).PlayTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _outpost, new Army() { Dot = 2 }),
            ("Player02", _player2Start, _center, new Army() { Square = 10 })
        ]));
    }

    /// <summary>
    /// The squares march into the home of Player01, kill the 3 dots there and destroy the plane,
    /// the only building of Player01. The outpost takes the given order in the same turn.
    /// </summary>
    private static TurnResult Raid(Command? outpostOrder = null)
    {
        List<(string, Hexagon, Command)> commands = [("Player02", _center, Move(_player1Start, new Army() { Square = 10 }))];
        if (outpostOrder is not null)
        {
            commands.Add(("Player01", _outpost, outpostOrder));
        }
        GameState before = SquaresInTheCenter();
        var orders = DictionaryExtensions.CommandsToDictionary(commands);
        TurnResult result = before.ResolveTurn(orders);
        StateInvariants.CheckTransition(before, orders, result).ShouldBeEmpty();
        return result;
    }

    [Test]
    public void LosingTheLastBuilding_DefeatsThePlayerAndEndsTheGame()
    {
        TurnResult result = Raid();

        result.Report.Defeats.ShouldBe(["Player01"]);
        GameStanding standing = Standing.Of(result.State);
        standing.DefeatedPlayerIds.ShouldBe(["Player01"]);
        standing.Outcome.Value.ShouldBe(new Victory("Player02"));
        standing.IsGameOver.ShouldBeTrue();
    }

    [Test]
    public void Defeat_IsReportedToEveryPlayerAndShownInEveryView()
    {
        TurnResult result = Raid();

        foreach (string playerId in result.State.PlayerIds)
        {
            PlayerGameState view = FogOfWar.GetPlayerGameState(result, playerId);
            view.Report.Defeats.ShouldBe(["Player01"]);
            view.Standing.DefeatedPlayerIds.ShouldBe(["Player01"]);
            view.Standing.Outcome.Value.ShouldBe(new Victory("Player02"));
        }
    }

    [Test]
    public void Defeat_IsReportedOnlyInTheTurnItHappens()
    {
        TurnResult next = Raid().State.ResolveTurn(new Dictionary<string, Dictionary<Hexagon, Command>>());

        next.Report.Defeats.ShouldBeEmpty();
        Standing.Of(next.State).DefeatedPlayerIds.ShouldBe(["Player01"]);
    }

    [Test]
    public void GameWithBuildingsOnBothSides_IsRunningWithoutDefeatedPlayers()
    {
        GameStanding standing = Standing.Of(SquaresInTheCenter());

        standing.DefeatedPlayerIds.ShouldBeEmpty();
        standing.Outcome.Value.ShouldBeOfType<GameRunning>();
        standing.IsGameOver.ShouldBeFalse();
    }

    [Test]
    public void SimultaneousBaseTrade_DefeatsBothPlayersWithoutAWinner()
    {
        // A line of 3 hexagons; the squares cross it in one turn and pass each other on the way.
        GameSettings line = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01", army: new Army() { Square = 20 }))
            .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 20 })));
        GameSettings gameSettings = line with { MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 } };

        TurnResult result = new GameState(gameSettings).ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _player2Start, new Army() { Square = 20 }),
            ("Player02", _player2Start, _player1Start, new Army() { Square = 20 })
        ]));

        result.Report.Defeats.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        GameStanding standing = Standing.Of(result.State);
        standing.DefeatedPlayerIds.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        standing.Outcome.Value.ShouldBeOfType<MutualDefeat>();
        standing.IsGameOver.ShouldBeTrue();
    }

    [Test]
    public void PendingConstruction_KeepsThePlayerInTheGame()
    {
        // Arrange and act: the outpost starts an axis in the turn the plane falls.
        TurnResult result = Raid(new Command() { Construction = new Compound() { Axis = 1 } });

        // Assert: no building, but a construction, so Player01 is not defeated and may order.
        PlayerState player = result.State.PlayerStates["Player01"];
        player.Compounds.Values.ShouldAllBe(x => x.IsEmpty);
        player.Constructions.On(_outpost).ShouldBe(new Compound() { Axis = 1 });
        result.Report.Defeats.ShouldBeEmpty();
        Standing.Of(result.State).Outcome.Value.ShouldBeOfType<GameRunning>();
        var orders = Command.Create((_outpost, new Command() { Construction = new Compound() { Axis = 1 } }));
        CommandValidator.Validate(result.State, "Player01", orders).ShouldBeEmpty();

        // The axis completes in the next turn.
        GameState next = result.State.PlayEmptyTurn();
        next.PlayerStates["Player01"].Compounds[_outpost].ShouldBe(new Compound() { Axis = 1 });
        Standing.Of(next).DefeatedPlayerIds.ShouldBeEmpty();
    }

    [Test]
    public void Defeat_CancelsThePendingResearches()
    {
        // The research runs on the outpost, where the dots are alive.
        TurnResult result = Raid(new Command() { Research = Upgrade.TriangleExponent });

        result.Report.Defeats.ShouldBe(["Player01"]);
        result.Report.Activities["Player01"].CanceledResearches.ShouldHaveSingleItem()
            .ShouldBe(new KeyValuePair<Hexagon, Upgrade>(_outpost, Upgrade.TriangleExponent));
        result.State.PlayerStates["Player01"].Researches.ShouldBeEmpty();
    }

    [Test]
    public void OrderOfDefeatedPlayer_YieldsPlayerDefeated()
    {
        GameState state = Raid().State;
        var orders = Command.Create((_outpost, Move(_player1Start, new Army() { Dot = 1 })));

        var validations = CommandValidator.Validate(state, "Player01", orders).ToList();
        var withView = CommandValidator.Validate(FogOfWar.GetPlayerGameState(state, "Player01"), orders).ToList();

        validations.ShouldHaveSingleItem().Value.ShouldBe(new PlayerDefeated("Player01"));
        withView.ShouldHaveSingleItem().Value.ShouldBe(new PlayerDefeated("Player01"));
    }

    [Test]
    public void ConstructionOfDefeatedPlayer_YieldsOnlyPlayerDefeated()
    {
        GameState state = Raid().State;
        var orders = Command.Create((_outpost, new Command() { Construction = new Compound() { Plane = 1 } }));

        CommandValidator.Validate(state, "Player01", orders).ShouldHaveSingleItem().Value.ShouldBe(new PlayerDefeated("Player01"));
    }

    [Test]
    public void DefeatedPlayerWithoutOrders_IsValid()
    {
        GameState state = Raid().State;

        CommandValidator.Validate(state, "Player01", _noOrders).ShouldBeEmpty();
        CommandValidator.Validate(state, "Player01", Command.Create((_outpost, new Command()))).ShouldBeEmpty();
    }

    [Test]
    public void ArmyOfDefeatedPlayer_StaysAndFightsWhenAttacked()
    {
        // Arrange
        GameState state = Raid().State;
        state.PlayerStates["Player01"].Armies[_outpost].ShouldBe(new Army() { Dot = 2 });
        Army squares = state.PlayerStates["Player02"].Armies[_player1Start];

        // Act: the squares move on to the outpost.
        TurnResult result = state.ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player1Start, _outpost, squares)
        ]));

        // Assert
        FightReport fight = result.Report.Fights[_outpost];
        fight.Armies["Player01"].ShouldBe(new Army() { Dot = 2 });
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 2 });
        result.State.PlayerStates["Player01"].Armies.GetValueOrDefault(_outpost).ShouldBe(Army.Empty);
    }
}
