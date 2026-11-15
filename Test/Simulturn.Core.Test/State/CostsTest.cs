using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public sealed class CostsTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly ImmutableDictionary<Upgrade, byte> _noUpgrades = ImmutableDictionary<Upgrade, byte>.Empty;

    /// <summary>
    /// A map of 3 hexagons in a line. Player01 starts with 5 dots, a plane and a cube and can
    /// research the triangle upgrade at home. The start hexagons hold no matter, so nobody has income.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01", matter: 0, compound: new Compound() { Plane = 1, Cube = 1 }).WithResearch(Upgrade.TriangleExponent))
        .SetItem(_player2Start, Maps.Start("Player02", matter: 0)));

    [Test]
    public void Training_CostsMatterAndSpacePerUnit()
    {
        Costs.Training(_gameSettings, new Army() { Dot = 2, Square = 1 }).ShouldBe(new OrderCost(Matter: 2 * 75 + 200, Space: 2 * 1 + 3));
        Costs.Training(_gameSettings, Unit.Square).ShouldBe(new OrderCost(Matter: 200, Space: 3));
    }

    [Test]
    public void Construction_CostsMatterAndNoSpace()
    {
        Costs.Construction(_gameSettings, new Compound() { Axis = 2, Plane = 1 }).ShouldBe(new OrderCost(Matter: 2 * 150 + 350, Space: 0));
        Costs.Construction(_gameSettings, Building.Plane).ShouldBe(new OrderCost(Matter: 350, Space: 0));
    }

    [Test]
    public void Research_CostsTheLevelAfterTheCompletedOnes()
    {
        var levelOne = _noUpgrades.Add(Upgrade.TriangleExponent, 1);
        var levelTwo = _noUpgrades.Add(Upgrade.TriangleExponent, 2);

        Costs.Research(_gameSettings, _noUpgrades, Upgrade.TriangleExponent).ShouldBe(new OrderCost(150, 0));
        Costs.Research(_gameSettings, levelOne, Upgrade.TriangleExponent).ShouldBe(new OrderCost(175, 0));
        Costs.Research(_gameSettings, levelTwo, Upgrade.TriangleExponent).ShouldBe(new OrderCost(0, 0));
    }

    [Test]
    public void CommandWithMovementOnly_IsFree()
    {
        Command command = new()
        {
            MovementCommands = [new MovementCommand() { Destination = new Hexagon(0, 0), Army = new Army() { Dot = 5 } }]
        };

        Costs.Of(_gameSettings, _noUpgrades, command).ShouldBe(new OrderCost(0, 0));
    }

    [Test]
    public void OrdersOfATurn_CostTheSumOverTheHexagons()
    {
        var commands = Command.Create(
            (_player1Start, new Command() { Training = new Army() { Dot = 1 }, Research = Upgrade.TriangleExponent }),
            (new Hexagon(0, 0), new Command() { Construction = new Compound() { Axis = 1 } }));

        Costs.Of(_gameSettings, _noUpgrades, commands).ShouldBe(new OrderCost(Matter: 75 + 150 + 150, Space: 1));
    }

    [Test]
    public void TurnResolution_PaysExactlyTheCostOfTheOrders()
    {
        // Arrange: a dot, a square and the triangle upgrade for 75 + 200 + 150 of the 500 matter.
        GameState state = new(_gameSettings);
        var commands = Command.Create((_player1Start, new Command()
        {
            Training = new Army() { Dot = 1, Square = 1 },
            Research = Upgrade.TriangleExponent
        }));
        PlayerState before = state.PlayerStates["Player01"];
        OrderCost cost = Costs.Of(_gameSettings, before.UpgradeLevels, commands);

        // Act
        TurnResult result = state.ResolveTurn(new Dictionary<string, Dictionary<Hexagon, Command>>() { { "Player01", commands } });

        // Assert
        cost.ShouldBe(new OrderCost(Matter: 425, Space: 4));
        PlayerState after = result.State.PlayerStates["Player01"];
        after.Matter.ShouldBe(before.Matter - cost.Matter);
        after.UsedSpace.ShouldBe(before.UsedSpace + cost.Space);
    }

    [Test]
    public void OrdersBeyondTheMatter_YieldInsufficientMatterWithTheCostOfTheOrders()
    {
        GameState state = new(_gameSettings with { StartMatter = 400 });
        var commands = Command.Create((_player1Start, new Command()
        {
            Training = new Army() { Dot = 1 },
            Construction = new Compound() { Plane = 1 }
        }));
        OrderCost cost = Costs.Of(_gameSettings, _noUpgrades, commands);

        var validations = CommandValidator.Validate(state, "Player01", commands).ToList();

        validations.ShouldHaveSingleItem().Value.ShouldBe(new InsufficientMatter("Player01", cost.Matter, 400));
    }

    [Test]
    public void TrainingBeyondTheSpace_YieldsInsufficientSpaceWithTheCostOfTheOrders()
    {
        // A plane provides 10 space, the 5 dots use 5 of it. A square and 3 dots need 6 more.
        GameState state = new(_gameSettings);
        var commands = Command.Create((_player1Start, new Command() { Training = new Army() { Square = 1, Dot = 3 } }));
        OrderCost cost = Costs.Of(_gameSettings, _noUpgrades, commands);

        var validations = CommandValidator.Validate(state, "Player01", commands).ToList();

        cost.Space.ShouldBe(6);
        validations.Select(x => x.Value).ShouldContain(new InsufficientSpace("Player01", 5 + cost.Space, 10));
    }
}
