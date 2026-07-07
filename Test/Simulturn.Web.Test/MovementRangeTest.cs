using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class MovementRangeTest
{
    // Defaults: dots range 1, combat units range 2.
    private static GameState NewState() => new(DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1));

    private static Hexagon Start(GameState state) =>
        state.PlayerStates["Player 1"].Compounds.Keys.Single();

    [Test]
    public void GetDestinations_DotsOnly_HighlightsAdjacentHexes()
    {
        var state = NewState();
        var start = Start(state);
        var destinations = MovementRange.GetDestinations(state, start, new Army { Dot = 5 });
        destinations.ShouldNotBeEmpty();
        destinations.ShouldAllBe(hexagon => start.DistanceTo(hexagon) == 1);
    }

    [Test]
    public void GetDestinations_MixedArmy_UsesFastestUnit()
    {
        var state = NewState();
        var start = Start(state);
        var destinations = MovementRange.GetDestinations(state, start, new Army { Dot = 5, Triangle = 1 });
        destinations.Max(hexagon => start.DistanceTo(hexagon)).ShouldBe(2);
    }

    [Test]
    public void GetDestinations_NoUnits_IsEmpty()
    {
        var state = NewState();
        MovementRange.GetDestinations(state, Start(state), Army.Empty).ShouldBeEmpty();
    }

    [Test]
    public void ReachableArmy_FiltersUnitsThatAreTooSlow()
    {
        var state = NewState();
        var army = new Army { Dot = 5, Triangle = 2 };
        var reachable = MovementRange.ReachableArmy(state.GameSettings, army, distance: 2);
        reachable.Dot.ShouldBe((short)0);       // dot range 1 < 2
        reachable.Triangle.ShouldBe((short)2);  // triangle range 2
    }

    [Test]
    public void Session_OutOfRangeMovement_BlocksEndTurn()
    {
        var session = new GameSession();
        session.StartNewGame(DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1), GameMode.God);
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();
        var farAway = session.Current.Hexagons.First(hexagon => start.DistanceTo(hexagon) == 3);

        session.UpdateDraft("Player 1", start, draft => draft.UpsertMovement(farAway, new ArmyDraft { Dot = 1 }));

        session.ValidationResults["Player 1"].ShouldContain(validation =>
            validation is Simulturn.Core.Model.State.StateValidation.MovementExceedsRange);
        session.CanEndTurn.ShouldBeFalse();
    }
}
