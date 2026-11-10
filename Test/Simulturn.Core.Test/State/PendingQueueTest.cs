using System.Collections.Immutable;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// Trainings, Constructions and Researches only hold pending work: NextTurn removes an entry in the
/// turn it is applied, and IsValid reports entries that were left behind. The history of a game
/// lives in its commands, not in the queues (work item pending-only-queues).
/// </summary>
public sealed class PendingQueueTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. Player01 starts with a plane and a dome and can research the dot upgrade at home.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_start, Maps.Start("Player01", compound: new Compound() { Plane = 1, Dome = 1 }).WithResearch(Upgrade.DotUpgrade))
            .SetItem(_opponentStart, Maps.Start("Player02")),
        startMatter: 5000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Player1(Command command)
    {
        return new() { ["Player01"] = new() { [_start] = command } };
    }

    [Test]
    public void TrainingWithDurationOne_IsAppliedAndRemovedInTheSameTurn()
    {
        var state = new GameState(_gameSettings);

        var turn1 = state.PlayTurn(Player1(new Command() { Training = new Army() { Dot = 1 } }));

        turn1.PlayerStates["Player01"].Armies[_start].ShouldBe(new Army() { Dot = 6 });
        turn1.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
    }

    [Test]
    public void Training_IsRemovedFromQueueWhenItCompletes()
    {
        // Arrange: a circle takes two turns.
        var state = new GameState(_gameSettings);

        // Act
        var turn1 = state.PlayTurn(Player1(new Command() { Training = new Army() { Circle = 1 } }));
        var turn2 = turn1.PlayEmptyTurn();

        // Assert
        turn1.PlayerStates["Player01"].Trainings.ShouldHaveSingleItem().Key.ShouldBe((ushort)1);
        turn1.PlayerStates["Player01"].Armies[_start].ShouldBe(new Army() { Dot = 5 });
        turn2.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
        turn2.PlayerStates["Player01"].Armies[_start].ShouldBe(new Army() { Dot = 5, Circle = 1 });
    }

    [Test]
    public void Construction_IsRemovedFromQueueWhenItCompletes()
    {
        // Arrange: an axis takes two turns.
        var state = new GameState(_gameSettings);

        // Act
        var turn1 = state.PlayTurn(Player1(new Command() { Construction = new Compound() { Axis = 1 } }));
        var turn2 = turn1.PlayEmptyTurn();

        // Assert
        turn1.PlayerStates["Player01"].Constructions.ShouldHaveSingleItem().Key.ShouldBe((ushort)1);
        turn1.PlayerStates["Player01"].Compounds[_start].ShouldBe(new Compound() { Plane = 1, Dome = 1 });
        turn2.PlayerStates["Player01"].Constructions.ShouldBeEmpty();
        turn2.PlayerStates["Player01"].Compounds[_start].ShouldBe(new Compound() { Plane = 1, Dome = 1, Axis = 1 });
    }

    [Test]
    public void Research_IsRemovedFromQueueWhenItCompletes()
    {
        // Arrange: the first dot upgrade takes three turns.
        var state = new GameState(_gameSettings);

        // Act
        var turn1 = state.PlayTurn(Player1(new Command() { Research = Upgrade.DotUpgrade }));
        var turn2 = turn1.PlayEmptyTurn();
        var turn3 = turn2.PlayEmptyTurn();

        // Assert
        turn1.PlayerStates["Player01"].Researches.ShouldHaveSingleItem().Value.CompletionTurn.ShouldBe((ushort)2);
        turn2.PlayerStates["Player01"].Researches.ShouldHaveSingleItem().Value.CompletionTurn.ShouldBe((ushort)2);
        turn2.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        turn3.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        turn3.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)1);
    }

    [Test]
    public void IsValid_TrainingPastItsCompletionTurn_YieldsExpiredTraining()
    {
        var state = WithPlayer1(player => player with
        {
            Trainings = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>>.Empty
                .Add(0, ImmutableDictionary<Hexagon, Army>.Empty.Add(_start, new Army() { Dot = 1 }))
        });

        var validation = state.IsValid().ShouldHaveSingleItem().ShouldBeOfType<ExpiredTraining>();

        validation.PlayerId.ShouldBe("Player01");
        validation.CompletionTurn.ShouldBe((ushort)0);
    }

    [Test]
    public void IsValid_ConstructionPastItsCompletionTurn_YieldsExpiredConstruction()
    {
        var state = WithPlayer1(player => player with
        {
            Constructions = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>>.Empty
                .Add(0, ImmutableDictionary<Hexagon, Compound>.Empty.Add(_start, new Compound() { Axis = 1 }))
        });

        var validation = state.IsValid().ShouldHaveSingleItem().ShouldBeOfType<ExpiredConstruction>();

        validation.PlayerId.ShouldBe("Player01");
        validation.CompletionTurn.ShouldBe((ushort)0);
    }

    [Test]
    public void IsValid_ResearchPastItsCompletionTurn_YieldsExpiredResearch()
    {
        var state = WithPlayer1(player => player with
        {
            Researches = ImmutableDictionary<Hexagon, Research>.Empty.Add(_start, new Research(Upgrade.DotUpgrade, 0))
        });

        var validation = state.IsValid().ShouldHaveSingleItem().ShouldBeOfType<ExpiredResearch>();

        validation.PlayerId.ShouldBe("Player01");
        validation.CompletionTurn.ShouldBe((ushort)0);
    }

    [Test]
    public void IsValid_PendingEntryInQueue_YieldsNothing()
    {
        var state = WithPlayer1(player => player with
        {
            Trainings = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>>.Empty
                .Add(1, ImmutableDictionary<Hexagon, Army>.Empty.Add(_start, new Army() { Dot = 1 }))
        });

        state.IsValid().ShouldBeEmpty();
    }

    /// <summary>
    /// The state after one empty turn (turn 1), with the state of Player01 replaced. Only reachable
    /// through <see cref="GameState.FromParts"/>, because NextTurn never leaves an applied entry behind.
    /// </summary>
    private static GameState WithPlayer1(Func<PlayerState, PlayerState> change)
    {
        var state = new GameState(_gameSettings).PlayEmptyTurn();
        var playerStates = state.PlayerStates.SetItem("Player01", change(state.PlayerStates["Player01"]));
        return GameState.FromParts(state.GameSettings, state.Turn, playerStates, state.RemainingMatter);
    }
}
