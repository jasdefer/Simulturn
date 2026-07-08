using Simulturn.Core.Model;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class CombatForecastTest
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
    public void Evaluate_StrongerArmy_WinsWithFractionalLosses()
    {
        var settings = DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1);
        var forecast = CombatForecast.Evaluate(settings, "Player 2",
            new Army { Dot = 3 }, Army.Empty,
            new Army { Dot = 2 }, Army.Empty);

        forecast.OwnArmyWins.ShouldBeTrue();
        forecast.MutualDestruction.ShouldBeFalse();
        forecast.OwnLosses.Dot.ShouldBe((short)2); // ceil(3 · 2/3)
        forecast.OpponentLosses.Dot.ShouldBe((short)2); // loser is wiped out
    }

    [Test]
    public void Evaluate_EqualArmies_MutualDestruction()
    {
        var settings = DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1);
        var army = new Army { Dot = 5 };
        var forecast = CombatForecast.Evaluate(settings, "Player 2", army, Army.Empty, army, Army.Empty);

        forecast.MutualDestruction.ShouldBeTrue();
        forecast.OwnLosses.ShouldBe(army);
        forecast.OpponentLosses.ShouldBe(army);
    }

    [Test]
    public void Evaluate_MatchesEngineResolution_ForCollidingArmies()
    {
        var session = NewSession();
        var settings = session.Current.GameSettings;
        var center = new Hexagon(0, 0);
        session.UpdateDraft("Player 1", Start(session, "Player 1"),
            draft => draft.UpsertMovement(center, new ArmyDraft { Dot = 5 }));
        session.UpdateDraft("Player 2", Start(session, "Player 2"),
            draft => draft.UpsertMovement(center, new ArmyDraft { Dot = 2 }));

        var forecast = CombatForecast.Evaluate(settings, "Player 2",
            new Army { Dot = 5 },
            session.Current.PlayerStates["Player 1"].ExponentBonusFromUpgrades(settings),
            new Army { Dot = 2 },
            session.Current.PlayerStates["Player 2"].ExponentBonusFromUpgrades(settings));

        session.ResolveTurn();

        var report = session.Reports.ShouldHaveSingleItem();
        var player1Battle = report.Players.Single(player => player.PlayerId == "Player 1").Battles.ShouldHaveSingleItem();
        var player2Battle = report.Players.Single(player => player.PlayerId == "Player 2").Battles.ShouldHaveSingleItem();
        player1Battle.Losses.ShouldBe(forecast.OwnLosses);
        player2Battle.Losses.ShouldBe(forecast.OpponentLosses);
    }
}
