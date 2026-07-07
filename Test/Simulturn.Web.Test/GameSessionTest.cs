using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class GameSessionTest
{
    private static GameSession NewSession()
    {
        var session = new GameSession();
        session.StartNewGame(DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1), GameMode.God);
        return session;
    }

    [Test]
    public void StartNewGame_InitializesTurnZero()
    {
        var session = NewSession();
        session.HasGame.ShouldBeTrue();
        session.Current.Turn.ShouldBe((ushort)0);
        session.IsViewingPast.ShouldBeFalse();
        session.PlayerIds.ShouldBe(["Player 1", "Player 2"]);
        session.CanEndTurn.ShouldBeTrue();
    }

    [Test]
    public void ResolveTurn_WithoutOrders_AdvancesTurnAndAccruesIncome()
    {
        var session = NewSession();
        int matterBefore = session.Current.PlayerStates["Player 1"].Matter;
        session.ResolveTurn();
        session.Current.Turn.ShouldBe((ushort)1);
        // 5 idle dots on a matter hex with a Plane → income.
        session.Current.PlayerStates["Player 1"].Matter.ShouldBeGreaterThan(matterBefore);
        session.StateIssues.ShouldBeEmpty();
    }

    [Test]
    public void ResolveTurn_KeepsFullHistory()
    {
        var session = NewSession();
        session.ResolveTurn();
        session.ResolveTurn();
        session.History.Count.ShouldBe(3);
        session.History[0].Turn.ShouldBe((ushort)0);
        session.History[2].Turn.ShouldBe((ushort)2);
    }

    [Test]
    public void ViewTurn_ShowsPastState_AndBlocksResolution()
    {
        var session = NewSession();
        session.ResolveTurn();
        session.ResolveTurn();
        session.ViewTurn(1);
        session.IsViewingPast.ShouldBeTrue();
        session.Viewed.Turn.ShouldBe((ushort)1);
        session.CanEndTurn.ShouldBeFalse();
        session.ResolveTurn();
        session.History.Count.ShouldBe(3); // unchanged
    }

    [Test]
    public void BranchFromViewed_DiscardsFutureTurns()
    {
        var session = NewSession();
        session.ResolveTurn();
        session.ResolveTurn();
        session.ResolveTurn();
        session.ViewTurn(1);
        session.BranchFromViewed();
        session.History.Count.ShouldBe(2);
        session.Current.Turn.ShouldBe((ushort)1);
        session.IsViewingPast.ShouldBeFalse();
        session.CanEndTurn.ShouldBeTrue();
    }

    [Test]
    public void UpdateDraft_TrainingDots_StaysValid_AndResolvesIntoArmy()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();

        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 1);
        session.CanEndTurn.ShouldBeTrue();
        session.ValidationResults["Player 1"].ShouldBeEmpty();

        session.ResolveTurn();
        // Dot training duration is 1 → completes immediately.
        session.Current.PlayerStates["Player 1"].Armies[start].Dot.ShouldBe((short)6);
        session.Drafts.ShouldBeEmpty();
    }

    [Test]
    public void UpdateDraft_UnaffordableTraining_ReportsValidationError()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();

        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 500); // 500 · 75 matter ≫ 500 start matter
        session.ValidationResults["Player 1"].ShouldNotBeEmpty();
        session.CanEndTurn.ShouldBeFalse();
    }

    [Test]
    public void UpdateDraft_EmptiedDraft_IsPruned()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();
        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 1);
        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 0);
        session.Drafts["Player 1"].ShouldBeEmpty();
    }

    [Test]
    public void RestartSameSettings_ReproducesIdenticalStart()
    {
        var session = NewSession();
        var startState = session.Current;
        session.ResolveTurn();
        session.RestartSameSettings();
        session.Current.Turn.ShouldBe((ushort)0);
        session.Current.PlayerStates["Player 1"].Matter.ShouldBe(startState.PlayerStates["Player 1"].Matter);
        session.History.Count.ShouldBe(1);
    }

    private static Simulturn.Core.Model.Hexagon NeighborOf(GameSession session, Simulturn.Core.Model.Hexagon source) =>
        session.Current.Hexagons.First(hexagon => hexagon.DistanceTo(source) == 1);

    [Test]
    public void AvailableArmyForMovement_SubtractsStagedMovements()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();
        var destination = NeighborOf(session, start);

        session.UpdateDraft("Player 1", start, draft => draft.UpsertMovement(destination, new ArmyDraft { Dot = 3 }));

        session.AvailableArmyForMovement("Player 1", start).Dot.ShouldBe((short)2);
        // Editing the existing order excludes it from the reservation.
        session.AvailableArmyForMovement("Player 1", start, destination).Dot.ShouldBe((short)5);
    }

    [Test]
    public void StagedMovement_ResolvesToDestination()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();
        var destination = NeighborOf(session, start); // dots have movement range 1

        session.UpdateDraft("Player 1", start, draft => draft.UpsertMovement(destination, new ArmyDraft { Dot = 2 }));
        session.CanEndTurn.ShouldBeTrue();
        session.ResolveTurn();

        session.Current.PlayerStates["Player 1"].Armies[start].Dot.ShouldBe((short)3);
        session.Current.PlayerStates["Player 1"].Armies[destination].Dot.ShouldBe((short)2);
    }

    [Test]
    public void ClearDrafts_RemovesAllOrdersAndRevalidates()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();
        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 500); // invalid: too expensive
        session.CanEndTurn.ShouldBeFalse();

        session.ClearDrafts("Player 1");

        session.Drafts.ShouldNotContainKey("Player 1");
        session.ValidationResults["Player 1"].ShouldBeEmpty();
        session.CanEndTurn.ShouldBeTrue();
    }

    [Test]
    public void ProjectedIncome_MatchesEngineResolution()
    {
        var session = NewSession();
        int projected = session.ProjectedIncome("Player 1");
        projected.ShouldBe(50); // 5 idle dots × 10 income

        int matterBefore = session.Current.PlayerStates["Player 1"].Matter;
        session.ResolveTurn();
        session.Current.PlayerStates["Player 1"].Matter.ShouldBe(matterBefore + projected);
    }

    [Test]
    public void ProjectedIncome_AccountsForStagedConstructions()
    {
        var session = NewSession();
        var start = session.Current.PlayerStates["Player 1"].Compounds.Keys.Single();

        // Each staged construction occupies one dot, reducing income by 10.
        session.UpdateDraft("Player 1", start, draft => draft.Construction.Axis = 2);
        int projected = session.ProjectedIncome("Player 1");
        projected.ShouldBe(30);

        int matterBefore = session.Current.PlayerStates["Player 1"].Matter;
        int constructionCost = 2 * 100; // Axis costs 100
        session.ResolveTurn();
        session.Current.PlayerStates["Player 1"].Matter.ShouldBe(matterBefore + projected - constructionCost);
    }

    [Test]
    public void Changed_IsRaisedOnMutations()
    {
        var session = new GameSession();
        int raised = 0;
        session.Changed += () => raised++;
        session.StartNewGame(DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1), GameMode.God);
        session.ResolveTurn();
        raised.ShouldBe(2);
    }
}
