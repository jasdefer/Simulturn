using Simulturn.Core.Model;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class TurnReportServiceTest
{
    private static GameSession NewSession()
    {
        var session = new GameSession();
        session.StartNewGame(DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1), GameMode.God);
        return session;
    }

    private static Hexagon Start(GameSession session, string playerId) =>
        session.Current.PlayerStates[playerId].Compounds.Keys.Single();

    [Test]
    public void Report_IncludesIncomeAndMatterLedger()
    {
        var session = NewSession();
        session.ResolveTurn();

        var report = session.Reports.ShouldHaveSingleItem();
        report.FromTurn.ShouldBe((ushort)0);
        report.ToTurn.ShouldBe((ushort)1);
        foreach (var player in report.Players)
        {
            var income = player.Income.ShouldHaveSingleItem();
            income.Amount.ShouldBe(50); // 5 idle dots × 10 income
            player.MatterAfter.ShouldBe(player.MatterBefore + 50);
        }
    }

    [Test]
    public void Report_DurationOneTraining_IsACompletion()
    {
        var session = NewSession();
        var start = Start(session, "Player 1");
        session.UpdateDraft("Player 1", start, draft => draft.Training.Dot = 1);
        session.ResolveTurn();

        var player = session.Reports[0].Players.Single(player => player.PlayerId == "Player 1");
        var completion = player.CompletedTrainings.ShouldHaveSingleItem();
        completion.Hexagon.ShouldBe(start);
        completion.Army.Dot.ShouldBe((short)1);
    }

    [Test]
    public void Report_MultiTurnConstruction_CompletesInLaterReport()
    {
        var session = NewSession();
        var start = Start(session, "Player 1");
        session.UpdateDraft("Player 1", start, draft => draft.Construction.Axis = 1); // duration 2
        session.ResolveTurn();
        session.Reports[0].Players.Single(player => player.PlayerId == "Player 1")
            .CompletedConstructions.ShouldBeEmpty();

        session.ResolveTurn();
        var completion = session.Reports[1].Players.Single(player => player.PlayerId == "Player 1")
            .CompletedConstructions.ShouldHaveSingleItem();
        completion.Hexagon.ShouldBe(start);
        completion.Compound.Axis.ShouldBe((short)1);
    }

    [Test]
    public void Report_MovementsAreRecorded()
    {
        var session = NewSession();
        var start = Start(session, "Player 1");
        var center = new Hexagon(0, 0);
        session.UpdateDraft("Player 1", start, draft => draft.UpsertMovement(center, new ArmyDraft { Dot = 2 }));
        session.ResolveTurn();

        var movement = session.Reports[0].Players.Single(player => player.PlayerId == "Player 1")
            .Movements.ShouldHaveSingleItem();
        movement.From.ShouldBe(start);
        movement.To.ShouldBe(center);
        movement.Army.Dot.ShouldBe((short)2);
    }

    [Test]
    public void Report_CollidingArmies_ProduceBattleLosses()
    {
        var session = NewSession();
        var center = new Hexagon(0, 0);
        foreach (var playerId in session.PlayerIds)
        {
            var start = Start(session, playerId);
            session.UpdateDraft(playerId, start, draft => draft.UpsertMovement(center, new ArmyDraft { Dot = 5 }));
        }
        session.ResolveTurn();

        foreach (var player in session.Reports[0].Players)
        {
            var battle = player.Battles.ShouldHaveSingleItem();
            battle.Hexagon.ShouldBe(center);
            battle.Losses.Dot.ShouldBe((short)5); // equal dot armies annihilate each other
        }
    }
}
