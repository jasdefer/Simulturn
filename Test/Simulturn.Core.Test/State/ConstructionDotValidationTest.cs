using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// A dot that builds stays: <see cref="CommandValidator.Validate"/> refuses a movement that leaves
/// fewer dots on a hexagon than it has pending and requested constructions. Without the rule valid
/// commands led to a state <see cref="StateValidator.Validate"/> refuses.
/// </summary>
public sealed class ConstructionDotValidationTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. Player01 starts with five dots and enough matter for every command.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_start, Maps.Start("Player01"))
            .SetItem(_opponentStart, Maps.Start("Player02")),
        startMatter: 5000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Player1(params (Hexagon Hexagon, Command Command)[] commands)
    {
        return new() { ["Player01"] = commands.ToDictionary(x => x.Hexagon, x => x.Command) };
    }

    private static Command MoveDots(short dots, Hexagon destination)
    {
        return new Command() { Movements = [new MovementOrder() { Army = new Army() { Dot = dots }, Destination = destination }] };
    }

    private static List<CommandValidation> Validate(GameState state, Dictionary<string, Dictionary<Hexagon, Command>> commands)
    {
        return CommandValidator.Validate(state, "Player01", commands["Player01"]).ToList();
    }

    /// <summary>
    /// An axis takes two turns, so it is still pending on the next turn.
    /// </summary>
    private static GameState StateWithPendingAxis()
    {
        var state = new GameState(_gameSettings)
            .PlayTurn(Player1((_start, new Command() { Construction = new Compound() { Axis = 1 } })));
        state.PlayerStates["Player01"].Constructions.ShouldNotBeEmpty();
        state.PlayerStates["Player01"].Armies[_start].Dot.ShouldBe((short)5);
        return state;
    }

    [Test]
    public void MovingEveryDotOffAPendingConstruction_YieldsMovementLeavesConstructionWithoutDot()
    {
        // Arrange
        var state = StateWithPendingAxis();

        // Act
        var validations = Validate(state, Player1((_start, MoveDots(5, _center))));

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MovementLeavesConstructionWithoutDot>();
        validation.Hexagon.ShouldBe(_start);
        validation.Constructions.ShouldBe(new Compound() { Axis = 1 });
        validation.RemainingDots.ShouldBe((short)0);
    }

    [Test]
    public void MovingTheFreeDotsOffAPendingConstruction_IsAccepted()
    {
        var state = StateWithPendingAxis();

        Validate(state, Player1((_start, MoveDots(4, _center)))).ShouldBeEmpty();
    }

    [Test]
    public void MovingADotOffARequestedConstruction_YieldsMovementLeavesConstructionWithoutDot()
    {
        // Arrange: two constructions and a movement of four of the five dots in the same command.
        var state = new GameState(_gameSettings);
        var commands = Player1((_start, MoveDots(4, _center) with { Construction = new Compound() { Axis = 1, Dome = 1 } }));

        // Act
        var validations = Validate(state, commands);

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MovementLeavesConstructionWithoutDot>();
        validation.Hexagon.ShouldBe(_start);
        validation.Constructions.ShouldBe(new Compound() { Axis = 1, Dome = 1 });
        validation.RemainingDots.ShouldBe((short)1);
    }

    [Test]
    public void ADotArrivingInTheSameTurn_DoesNotReplaceTheDotOfAConstruction()
    {
        // Arrange: a pending axis and three dots at the start, two dots on the center.
        var state = new GameState(_gameSettings)
            .PlayTurn(Player1((_start, MoveDots(2, _center) with { Construction = new Compound() { Axis = 1 } })));

        // Act: all three dots leave the start while the two others come back.
        var validations = Validate(state, Player1((_start, MoveDots(3, _center)), (_center, MoveDots(2, _start))));

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MovementLeavesConstructionWithoutDot>();
        validation.Hexagon.ShouldBe(_start);
        validation.RemainingDots.ShouldBe((short)0);
    }

    [Test]
    public void MovingOtherUnitsOffAPendingConstruction_IsAccepted()
    {
        // Arrange: a triangle next to the dots, one dot per construction.
        var settings = TestSettings.Create(Maps.Line(-1, 1)
                .SetItem(_start, Maps.Start("Player01", army: new Army() { Dot = 1, Triangle = 1 }))
                .SetItem(_opponentStart, Maps.Start("Player02")),
            startMatter: 5000);
        var state = new GameState(settings)
            .PlayTurn(Player1((_start, new Command() { Construction = new Compound() { Axis = 1 } })));

        // Act
        var commands = Player1((_start, new Command()
        {
            Movements = [new MovementOrder() { Army = new Army() { Triangle = 1 }, Destination = _center }]
        }));

        // Assert
        Validate(state, commands).ShouldBeEmpty();
    }
}
