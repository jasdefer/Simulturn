using Simulturn.Core.Model;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class AiDrivingTest
{
    private static readonly string[] _players = ["Player 1", "Player 2"];

    private static GameSession HumanVsAi(GameMode mode, string aiId)
    {
        var session = new GameSession();
        var assignments = new Dictionary<string, AiKind> { ["Player 2"] = AiCatalog.Find(aiId)! };
        session.StartNewGame(DefaultSettings.Standard(_players, seed: 1), mode, assignments);
        return session;
    }

    [Test]
    public void IsAi_ReflectsAssignment()
    {
        var session = HumanVsAi(GameMode.God, "commander");
        session.IsAi("Player 2").ShouldBeTrue();
        session.IsAi("Player 1").ShouldBeFalse();
        session.AiName("Player 2").ShouldBe("Commander");
        session.HumanPlayerIds.ShouldBe(["Player 1"]);
    }

    [Test]
    public void GodMode_ActivePlayerIsHumanNotAi()
    {
        var session = HumanVsAi(GameMode.God, "commander");
        session.ActivePlayerId.ShouldBe("Player 1");
        session.SetActivePlayer("Player 2"); // refused — AI is not editable
        session.ActivePlayerId.ShouldBe("Player 1");
    }

    [Test]
    public void GodMode_ResolveTurn_GeneratesAiCommands()
    {
        var session = HumanVsAi(GameMode.God, "commander");
        // The human does nothing; the Commander should still act (it trains/harvests from turn 1).
        session.ResolveTurn();
        session.Current.Turn.ShouldBe((ushort)1);
        session.AiTurnErrors.ShouldBeEmpty();
        // Over a few turns the AI accrues matter or builds — assert it is doing *something*
        // by checking the game advances cleanly and stays engine-consistent.
        for (int i = 0; i < 5; i++)
        {
            session.ResolveTurn();
        }
        session.Current.Turn.ShouldBe((ushort)6);
        session.StateIssues.ShouldBeEmpty();
    }

    [Test]
    public void EveryCatalogAi_ProducesValidCommands_ForSeveralTurns()
    {
        foreach (var ai in AiCatalog.All)
        {
            var session = new GameSession();
            var assignments = new Dictionary<string, AiKind>
            {
                ["Player 1"] = ai,
                ["Player 2"] = AiCatalog.Find("random")!,
            };
            session.StartNewGame(DefaultSettings.Standard(_players, seed: 3), GameMode.God, assignments);
            for (int turn = 0; turn < 8 && !session.Current.IsGameOver; turn++)
            {
                session.ResolveTurn();
                session.AiTurnErrors.ShouldBeEmpty($"{ai.Name} should never produce invalid commands under the default (range-limited) settings");
                session.StateIssues.ShouldBeEmpty();
            }
        }
    }

    [Test]
    public void HotSeat_SkipsAiInRotation_AndResolvesAfterHumanSubmits()
    {
        var session = HumanVsAi(GameMode.HotSeat, "commander");
        // Only the human (Player 1) takes a seat.
        session.ActivePlayerId.ShouldBe("Player 1");
        session.ShowHandoff.ShouldBeTrue();
        session.BeginPlayerTurn();

        session.SubmitActivePlayer(); // last (only) human → resolves, AI acts at resolution
        session.Current.Turn.ShouldBe((ushort)1);
        session.ActivePlayerId.ShouldBe("Player 1"); // back to the human for the next turn
        session.AiTurnErrors.ShouldBeEmpty();
    }

    [Test]
    public void RestartSameSettings_KeepsAiAssignments()
    {
        var session = HumanVsAi(GameMode.God, "tactician");
        session.ResolveTurn();
        session.RestartSameSettings();
        session.Current.Turn.ShouldBe((ushort)0);
        session.IsAi("Player 2").ShouldBeTrue();
        session.AiName("Player 2").ShouldBe("Tactician");
    }

    [Test]
    public void HumanDrafts_ForAiPlayer_AreIgnored()
    {
        var session = HumanVsAi(GameMode.God, "idle");
        var aiStart = session.Current.PlayerStates["Player 2"].Compounds.Keys.Single();
        // Stage a bogus draft under the AI player's id; BuildCommands must override it with the AI's.
        session.UpdateDraft("Player 2", aiStart, draft => draft.Training.Dot = 1);
        session.ResolveTurn();
        // Idle AI trains nothing, so Player 2 still has exactly its starting 5 dots.
        session.Current.PlayerStates["Player 2"].Armies[aiStart].Dot.ShouldBe((short)5);
    }
}
