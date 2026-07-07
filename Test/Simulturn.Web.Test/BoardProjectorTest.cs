using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Web.Board;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class BoardProjectorTest
{
    private static readonly string[] _players = ["Player 1", "Player 2"];

    private static GameState NewState() => new(DefaultSettings.Standard(_players, seed: 1));

    [Test]
    public void ProjectGodMode_ShowsEveryHexFullyVisible()
    {
        var state = NewState();
        var board = new BoardProjector().ProjectGodMode(state);
        board.Hexes.Length.ShouldBe(state.Hexagons.Count);
        board.Hexes.ShouldAllBe(hex => hex.Visibility == Visibility.Occupied);
    }

    [Test]
    public void ProjectGodMode_PlacesOccupantsWithArmiesAndCompounds()
    {
        var state = NewState();
        var board = new BoardProjector().ProjectGodMode(state);
        var occupiedHexes = board.Hexes.Where(hex => hex.Occupants.Length > 0).ToList();
        occupiedHexes.Count.ShouldBe(2);
        foreach (var hex in occupiedHexes)
        {
            var occupant = hex.Occupants.ShouldHaveSingleItem();
            occupant.Army!.Value.Dot.ShouldBe((short)5);
            occupant.Compound.Plane.ShouldBe((short)1);
        }
    }

    [Test]
    public void ProjectGodMode_HexOrderIsDeterministic()
    {
        var state = NewState();
        var projector = new BoardProjector();
        var first = projector.ProjectGodMode(state).Hexes.Select(hex => hex.Hex).ToList();
        var second = projector.ProjectGodMode(state).Hexes.Select(hex => hex.Hex).ToList();
        second.ShouldBe(first);
    }

    [Test]
    public void PlayerIndices_AreOrderedByPlayerId()
    {
        var indices = BoardProjector.GetPlayerIndices(NewState());
        indices["Player 1"].ShouldBe((byte)0);
        indices["Player 2"].ShouldBe((byte)1);
    }

    [Test]
    public void ProjectGodMode_StagedMovement_ProducesArrowAndInboundGhost()
    {
        var state = NewState();
        var start = state.PlayerStates["Player 1"].Compounds.Keys.Single();
        var destination = new Hexagon(0, 0);

        var draft = new DraftCommand();
        draft.UpsertMovement(destination, new ArmyDraft { Dot = 2 });
        var drafts = new Dictionary<string, Dictionary<Hexagon, DraftCommand>>
        {
            ["Player 1"] = new() { [start] = draft },
        };

        var board = new BoardProjector().ProjectGodMode(state, drafts);

        var arrow = board.Arrows.ShouldHaveSingleItem();
        arrow.From.ShouldBe(start);
        arrow.To.ShouldBe(destination);
        arrow.Army.Dot.ShouldBe((short)2);
        arrow.Kind.ShouldBe(ArrowKind.Staged);

        var destinationHex = board.Hexes.Single(hex => hex.Hex == destination);
        var ghostOccupant = destinationHex.Occupants.ShouldHaveSingleItem();
        ghostOccupant.GhostInboundArmy.Dot.ShouldBe((short)2);
    }

    [Test]
    public void ProjectGodMode_ShowsInProgressQueues()
    {
        var state = NewState();
        var start = state.PlayerStates["Player 1"].Compounds.Keys.Single();

        // Start a construction (Axis, duration 2) and advance one turn so it is in progress.
        var draft = new DraftCommand();
        draft.Construction.Axis = 1;
        var commands = new Dictionary<string, Dictionary<Hexagon, Simulturn.Core.Model.Commands.Command>>
        {
            ["Player 1"] = new() { [start] = draft.ToCommand() },
        };
        var nextState = state.NextTurn(commands);

        var board = new BoardProjector().ProjectGodMode(nextState);
        var occupant = board.Hexes.Single(hex => hex.Hex == start).Occupants.ShouldHaveSingleItem();
        var queueItem = occupant.InProgress.ShouldHaveSingleItem();
        queueItem.Kind.ShouldBe(QueueKind.Construction);
        queueItem.Building.ShouldBe(Building.Axis);
        queueItem.TurnsRemaining.ShouldBe(1);
    }
}
