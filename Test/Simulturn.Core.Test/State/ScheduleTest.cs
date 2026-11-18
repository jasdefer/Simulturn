using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The completion turns of <see cref="Schedule"/> and that <see cref="TurnResolution.Resolve"/>
/// queues the orders with them.
/// </summary>
public sealed class ScheduleTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. Player01 starts with a plane and a dome and enough matter for every order.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_start, Maps.Start("Player01", compound: new Compound() { Plane = 1, Dome = 1 }))
            .SetItem(_opponentStart, Maps.Start("Player02")),
        startMatter: 5000);

    [Test]
    public void AnOrderWithADurationOfOne_CompletesInTheTurnItIsOrdered()
    {
        Schedule.CompletionTurn(turn: 7, duration: 1).ShouldBe((ushort)7);
        Schedule.CompletionTurn(turn: 7, duration: 3).ShouldBe((ushort)9);
    }

    [Test]
    public void TrainingAndConstruction_CompleteAfterTheDurationOfTheSettings()
    {
        Schedule.TrainingCompletion(_gameSettings, 4, Unit.Circle).ShouldBe((ushort)(4 + _gameSettings.TrainingDuration.Circle - 1));
        Schedule.ConstructionCompletion(_gameSettings, 4, Building.Axis).ShouldBe((ushort)(4 + _gameSettings.ConstructionDuration.Axis - 1));
    }

    [Test]
    public void Research_CompletesAfterTheDurationOfItsLevel()
    {
        var level = new UpgradeLevel(Cost: 100, Duration: 3, new StartMatterBonus(0));

        Schedule.ResearchCompletion(4, level).ShouldBe((ushort)6);
    }

    [Test]
    public void TurnResolution_QueuesTheOrdersWithTheCompletionTurnsOfTheSchedule()
    {
        // Arrange
        var state = new GameState(_gameSettings);
        var commands = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
            ["Player01"] = new()
            {
                [_start] = new Command() { Training = new Army() { Circle = 1 }, Construction = new Compound() { Axis = 1 } }
            }
        };

        // Act
        PlayerState player = state.PlayTurn(commands).PlayerStates["Player01"];

        // Assert
        player.Trainings.ShouldHaveSingleItem().Key
            .ShouldBe(new TurnHexagon(Schedule.TrainingCompletion(_gameSettings, state.Turn, Unit.Circle), _start));
        player.Constructions.ShouldHaveSingleItem().Key
            .ShouldBe(new TurnHexagon(Schedule.ConstructionCompletion(_gameSettings, state.Turn, Building.Axis), _start));
    }
}
