using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// Regression tests: Validate must yield validation records instead of throwing when the player
/// has pending queue entries at hexagons other than the one being commanded.
/// </summary>
public class ValidateRegressionTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _second = new(1, 0);

    private static readonly GameSettings _gameSettings = new()
    {
        Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
        ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
        CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
        ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
        FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
        Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
        MovementRange = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
        ProvidedSpace = new Compound() { Axis = 10, Dome = 0, Cube = 0, Plane = 10, Pyramid = 0 },
        RequiredSpace = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 1 },
        Seed = 1,
        StartMatter = 5000,
        PartialVisibilityRange = 2,
        VisibilityRange = 1,
        StructureDamage = new Army() { Triangle = 5, Circle = 5, Square = 5, Dot = 1 },
        TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
        StartUpgrades = ImmutableDictionary<string, ImmutableArray<Upgrade>>.Empty,
        Upgrades = new IUpgrade[]
        {
            new DotUpgrade() { Cost = 150, Duration = 3, ExponentBonus = 20 },
        }.ToDictionary(),
        HexagonSettings = new Dictionary<Hexagon, HexagonSettings>
        {
            {
                _start, new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                    PlayerInitialization = new ("Player01", new Army() { Dot = 5 }, new Compound() { Plane = 1, Dome = 1 })
                }
            },
            { _second, HexagonSettings.Empty },
            { new Hexagon(0, 0), HexagonSettings.Empty },
        }.ToImmutableDictionary(),
    };

    [Test]
    public void Validate_WithPendingTrainingAtAnotherHexagon_DoesNotThrow()
    {
        var gameState = new GameState(_gameSettings);

        // Queue a 2-turn training (Circle) at the start hexagon.
        var trainingCommands = new Dictionary<string, Dictionary<Hexagon, Command>>
        {
            ["Player01"] = new() { [_start] = new Command() { Training = new Army() { Circle = 1 } } },
        };
        gameState = gameState.NextTurn(trainingCommands);
        gameState.PlayerStates["Player01"].Trainings.ShouldNotBeEmpty();

        // Now validate a training command at a different hexagon: the pending-training lookup
        // for _second used to throw KeyNotFoundException.
        var commandsAtOtherHexagon = new Dictionary<Hexagon, Command>
        {
            [_second] = new Command() { Training = new Army() { Dot = 1 } },
        };
        var validations = Should.NotThrow(() => gameState.Validate("Player01", commandsAtOtherHexagon).ToList());

        // No Plane at _second → a MissingBuildingForTraining record, not an exception.
        validations.ShouldNotBeEmpty();
    }

    [Test]
    public void Validate_WithPendingConstructionAtAnotherHexagon_DoesNotThrow()
    {
        var gameState = new GameState(_gameSettings);

        // Queue a 2-turn construction (Axis) at the start hexagon.
        var constructionCommands = new Dictionary<string, Dictionary<Hexagon, Command>>
        {
            ["Player01"] = new() { [_start] = new Command() { Construction = new Compound() { Axis = 1 } } },
        };
        gameState = gameState.NextTurn(constructionCommands);
        gameState.PlayerStates["Player01"].Constructions.ShouldNotBeEmpty();

        var commandsAtOtherHexagon = new Dictionary<Hexagon, Command>
        {
            [_second] = new Command() { Construction = new Compound() { Axis = 1 } },
        };
        var validations = Should.NotThrow(() => gameState.Validate("Player01", commandsAtOtherHexagon).ToList());

        // No dots at _second → a MissingDotsForConstruction record, not an exception.
        validations.ShouldNotBeEmpty();
    }

    [Test]
    public void Validate_MovementWithinRange_YieldsNoRangeValidation()
    {
        var gameState = new GameState(_gameSettings);

        // Dot range is 1; the center hexagon is 1 away from the start.
        var commands = new Dictionary<Hexagon, Command>
        {
            [_start] = new Command()
            {
                MovementCommands = [new MovementCommand() { Destination = new Hexagon(0, 0), Army = new Army() { Dot = 2 } }],
            },
        };
        gameState.Validate("Player01", commands).ShouldBeEmpty();
    }

    [Test]
    public void Validate_MovementBeyondRange_YieldsMovementExceedsRange()
    {
        var gameState = new GameState(_gameSettings);

        // Dot range is 1; _second is 2 hexagons away from the start.
        var commands = new Dictionary<Hexagon, Command>
        {
            [_start] = new Command()
            {
                MovementCommands = [new MovementCommand() { Destination = _second, Army = new Army() { Dot = 2 } }],
            },
        };
        var validation = gameState.Validate("Player01", commands)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MovementExceedsRange>();
        validation.Unit.ShouldBe(Unit.Dot);
        validation.Distance.ShouldBe(2);
        validation.Range.ShouldBe((short)1);
        validation.Destination.ShouldBe(_second);
    }

    [Test]
    public void Validate_ConstructionWithoutAnyArmyAtHexagon_DoesNotThrow()
    {
        var gameState = new GameState(_gameSettings);

        // Constructing where the player has no army at all used to throw when building
        // the MissingDotsForConstruction record (direct Armies[hexagon] indexing).
        var commands = new Dictionary<Hexagon, Command>
        {
            [_second] = new Command() { Construction = new Compound() { Dome = 1 } },
        };
        var validations = Should.NotThrow(() => gameState.Validate("Player01", commands).ToList());
        validations.ShouldNotBeEmpty();
    }
}
