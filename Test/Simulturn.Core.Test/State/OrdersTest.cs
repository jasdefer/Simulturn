using Simulturn.Core.Engine;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// <see cref="Orders.Possible"/> and the <see cref="CommandsBuilder"/>: what limits an order and how an order entered
/// changes what is left. That everything offered is valid and nothing valid is missing is
/// checked in the random games (<see cref="OrderInvariants"/>).
/// </summary>
public sealed class OrdersTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);
    private static readonly ImmutableDictionary<Hexagon, Command> _noCommands = ImmutableDictionary<Hexagon, Command>.Empty;

    /// <summary>
    /// A line of three hexagons. Player01 starts with five dots and the given buildings.
    /// The triangle and the circle upgrade are researchable at its start, the triangle upgrade on the center.
    /// </summary>
    private static GameSettings Settings(Compound? compound = null, int startMatter = TestSettings.DefaultStartMatter, bool buildable = true)
    {
        return TestSettings.Create(Maps.Line(-1, 1)
                .SetItem(_start, Maps.Start("Player01", matter: 0, compound: compound).WithResearch(Upgrade.TriangleExponent, Upgrade.CircleExponent) with { IsBuildable = buildable })
                .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.TriangleExponent))
                .SetItem(_opponentStart, Maps.Start("Player02", matter: 0)),
            startMatter);
    }

    private static PlayerGameState View(GameState state)
    {
        return FogOfWar.GetPlayerGameState(state, "Player01");
    }

    private static Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> Player1(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        return new() { ["Player01"] = commands };
    }

    [Test]
    public void AtTheStart_EveryOrderIsLimitedByItsOwnRule()
    {
        // Arrange: 500 matter, five dots, one plane with space for ten units.
        var view = View(new GameState(Settings()));

        // Act
        PossibleOrders possible = Orders.Possible(view, _noCommands);

        // Assert
        possible.Budget.ShouldBe(new OrderCost(Matter: 500, Space: 5));
        PossibleHexagonOrders start = possible.Hexagons.ShouldHaveSingleItem().Value;
        start.Training.ShouldBe(new Army() { Dot = 1 }, "one plane trains one dot at a time");
        start.Construction.ShouldBe(new Compound() { Axis = 3, Dome = 5, Cube = 3, Pyramid = 3, Plane = 1 }, "five dots, the matter limits all but the dome");
        start.Research.ShouldBe([Upgrade.TriangleExponent, Upgrade.CircleExponent]);
        start.Movable.ShouldBe(new Army() { Dot = 5 });
    }

    [Test]
    public void AnOrderEntered_ShrinksWhatIsLeft()
    {
        // Arrange
        var view = View(new GameState(Settings()));

        // Act: the plane takes 350 of the 500 matter and one of the five dots.
        var commands = new CommandsBuilder().Construct(_start, new Compound() { Plane = 1 }).ToImmutable();
        PossibleOrders possible = Orders.Possible(view, commands);

        // Assert
        possible.Budget.ShouldBe(new OrderCost(Matter: 150, Space: 5));
        PossibleHexagonOrders start = possible.Hexagons[_start];
        start.Training.ShouldBe(new Army() { Dot = 1 });
        start.Construction.ShouldBe(new Compound() { Axis = 1, Dome = 1, Cube = 1, Pyramid = 1 });
        start.Research.ShouldBe([Upgrade.TriangleExponent, Upgrade.CircleExponent], "the first level costs the 150 matter left");
        start.Movable.ShouldBe(new Army() { Dot = 4 }, "the dot that builds stays");
    }

    [Test]
    public void Training_IsLimitedByTheFreeBuildings()
    {
        var view = View(new GameState(Settings(new Compound() { Plane = 3 }, startMatter: 5000)));

        Orders.Possible(view, _noCommands).Hexagons[_start].Training.ShouldBe(new Army() { Dot = 3 });

        var commands = new CommandsBuilder().Train(_start, new Army() { Dot = 2 }).ToImmutable();
        Orders.Possible(view, commands).Hexagons[_start].Training.ShouldBe(new Army() { Dot = 1 });
    }

    [Test]
    public void Training_IsLimitedByTheSpace()
    {
        // Arrange: one plane gives space for ten units, the five dots take five, a triangle takes three.
        var view = View(new GameState(Settings(new Compound() { Plane = 1, Pyramid = 5 }, startMatter: 5000)));

        // Act
        Army training = Orders.Possible(view, _noCommands).Hexagons[_start].Training;

        // Assert
        training.ShouldBe(new Army() { Dot = 1, Triangle = 1 });
    }

    [Test]
    public void Training_IsLimitedByTheMatter()
    {
        // Arrange: space for 25 more units and five pyramids, but matter for two triangles.
        var view = View(new GameState(Settings(new Compound() { Plane = 1, Axis = 2, Pyramid = 5 })));

        // Act
        Army training = Orders.Possible(view, _noCommands).Hexagons[_start].Training;

        // Assert
        training.ShouldBe(new Army() { Dot = 1, Triangle = 2 });
    }

    [Test]
    public void Construction_IsNotPossibleOnAHexagonThatIsNotBuildable()
    {
        var view = View(new GameState(Settings(buildable: false)));

        Orders.Possible(view, _noCommands).Hexagons[_start].Construction.ShouldBe(Compound.Empty);
    }

    [Test]
    public void Construction_IsLimitedByTheDotsThatStay()
    {
        // Arrange
        var view = View(new GameState(Settings(startMatter: 5000)));

        // Act: three of the five dots leave.
        var commands = new CommandsBuilder().Move(_start, _center, new Army() { Dot = 3 }).ToImmutable();
        PossibleHexagonOrders start = Orders.Possible(view, commands).Hexagons[_start];

        // Assert
        start.Construction.ShouldBe(new Compound() { Axis = 2, Dome = 2, Cube = 2, Pyramid = 2, Plane = 2 });
        start.Movable.ShouldBe(new Army() { Dot = 2 });
    }

    [Test]
    public void ARequestedResearch_KeepsOneDotAndLeavesNoSecondResearch()
    {
        // Arrange
        var view = View(new GameState(Settings(startMatter: 5000)));

        // Act
        var commands = new CommandsBuilder().Research(_start, Upgrade.TriangleExponent).ToImmutable();
        PossibleHexagonOrders start = Orders.Possible(view, commands).Hexagons[_start];

        // Assert
        start.Research.ShouldBeEmpty("a hexagon researches one upgrade at a time");
        start.Movable.ShouldBe(new Army() { Dot = 4 }, "the research needs a dot");
    }

    [Test]
    public void AnUpgradeRequestedOnOneHexagon_IsNotOfferedOnAnother()
    {
        // Arrange: a dot on the center, which offers the triangle upgrade too.
        var state = new GameState(Settings(startMatter: 5000))
            .PlayTurn(Player1(new CommandsBuilder().Move(_start, _center, new Army() { Dot = 1 }).ToImmutable()));
        var view = View(state);
        Orders.Possible(view, _noCommands).Hexagons[_center].Research.ShouldBe([Upgrade.TriangleExponent]);

        // Act
        var commands = new CommandsBuilder().Research(_start, Upgrade.TriangleExponent).ToImmutable();

        // Assert
        Orders.Possible(view, commands).Hexagons[_center].Research.ShouldBeEmpty();
    }

    [Test]
    public void TheLastDotOfAResearch_CanLeaveOnceAnotherDotIsOnItsWay()
    {
        // Arrange: one dot on the center with a pending research, four at the start.
        var state = new GameState(Settings(startMatter: 5000))
            .PlayTurn(Player1(new CommandsBuilder().Move(_start, _center, new Army() { Dot = 1 }).ToImmutable()))
            .PlayTurn(Player1(new CommandsBuilder().Research(_center, Upgrade.TriangleExponent).ToImmutable()));
        var view = View(state);
        Orders.Possible(view, _noCommands).Hexagons[_center].Movable.ShouldBe(Army.Empty);

        // Act
        var commands = new CommandsBuilder().Move(_start, _center, new Army() { Dot = 1 }).ToImmutable();

        // Assert
        Orders.Possible(view, commands).Hexagons[_center].Movable.ShouldBe(new Army() { Dot = 1 });
    }

    [Test]
    public void ADefeatedPlayer_CanOrderNothing()
    {
        // Arrange: no building and no construction left.
        PlayerGameState view = View(new GameState(Settings()));
        view = view with
        {
            PlayerState = view.PlayerState with { Compounds = ImmutableDictionary<Hexagon, Compound>.Empty, AvailableSpace = 0 }
        };

        // Act
        PossibleOrders possible = Orders.Possible(view, _noCommands);

        // Assert
        possible.Hexagons.ShouldBeEmpty();
    }

    [Test]
    public void Move_JoinsTheUnitsThatGoToTheSameDestination()
    {
        var commands = new CommandsBuilder()
            .Move(_start, _center, new Army() { Dot = 1 })
            .Move(_start, _center, new Army() { Dot = 2, Triangle = 1 })
            .Move(_start, _opponentStart, new Army() { Dot = 1 })
            .ToImmutable();

        commands[_start].Movements.ShouldBe([
            new MovementOrder() { Destination = _center, Army = new Army() { Dot = 3, Triangle = 1 } },
            new MovementOrder() { Destination = _opponentStart, Army = new Army() { Dot = 1 } }]);
    }

    [Test]
    public void TrainConstructAndResearch_AddToTheCommandOfTheHexagon()
    {
        var commands = new CommandsBuilder()
            .Train(_start, new Army() { Dot = 1 })
            .Train(_start, new Army() { Dot = 1, Circle = 1 })
            .Construct(_start, new Compound() { Axis = 1 })
            .Research(_start, Upgrade.CircleExponent)
            .ToImmutable();

        Command command = commands.ShouldHaveSingleItem().Value;
        command.Training.ShouldBe(new Army() { Dot = 2, Circle = 1 });
        command.Construction.ShouldBe(new Compound() { Axis = 1 });
        command.Research.ShouldBe(Upgrade.CircleExponent);
        command.Movements.ShouldBeEmpty();
    }
}
