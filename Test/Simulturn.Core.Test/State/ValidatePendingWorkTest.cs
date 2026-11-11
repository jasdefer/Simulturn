using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// Validate must report a validation instead of throwing when the player has pending work on a
/// hexagon other than the commanded one, or no army at all on the commanded hexagon.
/// Regression tests for a KeyNotFoundException fixed in PR #5 and #7.
/// </summary>
public sealed class ValidatePendingWorkTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. Player01 starts with a plane and a dome, so it can queue
    /// trainings that take more than one turn, and with enough matter for every command.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_start, Maps.Start("Player01", compound: new Compound() { Plane = 1, Dome = 1 }))
            .SetItem(_opponentStart, Maps.Start("Player02")),
        startMatter: 5000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Player1(Hexagon hexagon, Command command)
    {
        return new() { ["Player01"] = new() { [hexagon] = command } };
    }

    [Test]
    public void PendingTrainingOnAnotherHexagon_YieldsMissingBuildingForTraining()
    {
        // Arrange: a circle takes two turns, so it is still pending on the next turn.
        var state = new GameState(_gameSettings)
            .PlayTurn(Player1(_start, new Command() { Training = new Army() { Circle = 1 } }));
        state.PlayerStates["Player01"].Trainings.ShouldNotBeEmpty();

        // Act: train on a hexagon without any building.
        var commands = Player1(_center, new Command() { Training = new Army() { Dot = 1 } });
        var validations = Should.NotThrow(() => CommandValidator.Validate(state, "Player01", commands["Player01"]).ToList());

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MissingBuildingForTraining>();
        validation.Hexagon.ShouldBe(_center);
        validation.Unit.ShouldBe(Unit.Dot);
        validation.TrainedCount.ShouldBe((short)1);
        validation.BuildingCount.ShouldBe((short)0);
    }

    [Test]
    public void PendingConstructionOnAnotherHexagon_YieldsMissingDotsForConstruction()
    {
        // Arrange: an axis takes two turns, so it is still pending on the next turn.
        var state = new GameState(_gameSettings)
            .PlayTurn(Player1(_start, new Command() { Construction = new Compound() { Axis = 1 } }));
        state.PlayerStates["Player01"].Constructions.ShouldNotBeEmpty();

        // Act: construct on a hexagon without any own army.
        var commands = Player1(_center, new Command() { Construction = new Compound() { Axis = 1 } });
        var validations = Should.NotThrow(() => CommandValidator.Validate(state, "Player01", commands["Player01"]).ToList());

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MissingDotsForConstruction>();
        validation.Hexagon.ShouldBe(_center);
        validation.Constructions.ShouldBe(new Compound() { Axis = 1 });
        validation.AvailableDots.ShouldBe((short)0);
    }

    [Test]
    public void ConstructionWithoutAnyArmyOnTheHexagon_YieldsMissingDotsForConstruction()
    {
        var state = new GameState(_gameSettings);

        var commands = Player1(_center, new Command() { Construction = new Compound() { Dome = 1 } });
        var validations = Should.NotThrow(() => CommandValidator.Validate(state, "Player01", commands["Player01"]).ToList());

        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MissingDotsForConstruction>();
        validation.Hexagon.ShouldBe(_center);
        validation.Constructions.ShouldBe(new Compound() { Dome = 1 });
        validation.AvailableDots.ShouldBe((short)0);
    }

    [Test]
    public void PendingConstructionAtHomeAndDotOnAnotherHexagon_ValidatesTheOtherHexagonOnItsOwn()
    {
        // Arrange: a pending construction at home and a single dot on the center.
        var state = new GameState(_gameSettings)
            .PlayTurn(Player1(_start, new Command()
            {
                Construction = new Compound() { Pyramid = 1 },
                MovementCommands = [new MovementCommand() { Army = new Army() { Dot = 1 }, Destination = _center }]
            }));

        // Act: the dot can construct on the center, but nothing can be trained there.
        var commands = Player1(_center, new Command()
        {
            Construction = new Compound() { Dome = 1 },
            Training = new Army() { Dot = 1 }
        });
        var validations = Should.NotThrow(() => CommandValidator.Validate(state, "Player01", commands["Player01"]).ToList());

        // Assert
        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MissingBuildingForTraining>();
        validation.Hexagon.ShouldBe(_center);
        validation.Unit.ShouldBe(Unit.Dot);
    }
}
