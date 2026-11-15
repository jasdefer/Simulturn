using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public sealed class NeutralArmyTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends with 5 dots and a
    /// plane each, the center is guarded by the given neutral army.
    /// </summary>
    private static GameSettings GuardedCenter(Army guard)
    {
        return TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01"))
            .SetItem(_center, Maps.Hexagon().WithGuard(guard))
            .SetItem(_player2Start, Maps.Start("Player02")));
    }

    private static TurnResult Resolve(GameState state, params (string PlayerId, Hexagon Origin, Hexagon Destination, Army Army)[] movements)
    {
        var commands = DictionaryExtensions.MovementsToDictionary(movements);
        TurnResult result = state.ResolveTurn(commands);
        StateInvariants.CheckTransition(state, commands, result).ShouldBeEmpty();
        return result;
    }

    [Test]
    public void StartState_HoldsTheNeutralArmiesOfTheMap()
    {
        GameState state = new(GuardedCenter(new Army() { Dot = 3 }));

        state.NeutralArmies.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, Army>(_center, new Army() { Dot = 3 }));
        state.PlayerIds.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
    }

    [Test]
    public void MapWithoutGuards_HasNoNeutralArmies()
    {
        new GameState(GuardedCenter(Army.Empty)).NeutralArmies.ShouldBeEmpty();
    }

    [Test]
    public void ArmyEnteringAGuardedHexagon_FightsTheNeutralArmy()
    {
        // Arrange
        GameState state = new(GuardedCenter(new Army() { Dot = 3 }));

        // Act: 5 dots against 3 guards.
        TurnResult result = Resolve(state, ("Player01", _player1Start, _center, new Army() { Dot = 5 }));

        // Assert: the fight is reported like a fight between players, the guards have no upgrades.
        FightReport fight = result.Report.Fights.ShouldHaveSingleItem().Value;
        fight.Armies.Keys.ShouldBe(["Player01", Neutral.Id], ignoreOrder: true);
        fight.Armies[Neutral.Id].ShouldBe(new Army() { Dot = 3 });
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 3 });
        fight.Losses[Neutral.Id].ShouldBe(new Army() { Dot = 3 });
        fight.UpgradeLevels[Neutral.Id].ShouldBeEmpty();
        result.State.PlayerStates["Player01"].Armies[_center].ShouldBe(new Army() { Dot = 2 });
        result.State.NeutralArmies.ShouldBeEmpty();
    }

    [Test]
    public void NeutralArmyThatWins_KeepsItsSurvivors()
    {
        GameState state = new(GuardedCenter(new Army() { Dot = 5 }));

        TurnResult result = Resolve(state, ("Player01", _player1Start, _center, new Army() { Dot = 3 }));

        result.Report.Fights[_center].Losses[Neutral.Id].ShouldBe(new Army() { Dot = 3 });
        result.State.NeutralArmies[_center].ShouldBe(new Army() { Dot = 2 });
        result.State.PlayerStates["Player01"].Armies.GetValueOrDefault(_center).ShouldBe(Army.Empty);
    }

    [Test]
    public void FightAgainstGuards_IsWhatCombatForecastsFromTheSettings()
    {
        // Arrange: the settings tell every player what the guards consist of.
        GameSettings settings = GuardedCenter(new Army() { Triangle = 2, Circle = 1 });
        GameState state = new(settings);
        Army attackers = new() { Dot = 5 };
        FightReport forecast = Combat.Fight(settings, new Dictionary<string, Combatant>()
        {
            { "Player01", new Combatant() { Army = attackers } },
            { Neutral.Id, new Combatant() { Army = settings.HexagonSettings[_center].NeutralArmy } }
        });

        // Act
        TurnResult result = Resolve(state, ("Player01", _player1Start, _center, attackers));

        // Assert
        FightReport fight = result.Report.Fights[_center];
        fight.Losses["Player01"].ShouldBe(forecast.Losses["Player01"]);
        fight.Losses[Neutral.Id].ShouldBe(forecast.Losses[Neutral.Id]);
    }

    [Test]
    public void TwoPlayersEnteringAGuardedHexagon_FightEachOtherAndTheNeutralArmy()
    {
        GameState state = new(GuardedCenter(new Army() { Dot = 3 }));

        TurnResult result = Resolve(state,
            ("Player01", _player1Start, _center, new Army() { Dot = 5 }),
            ("Player02", _player2Start, _center, new Army() { Dot = 5 }));

        // The two players destroy each other, and 5 dots are enough against the 3 guards.
        FightReport fight = result.Report.Fights[_center];
        fight.Armies.Keys.ShouldBe(["Player01", "Player02", Neutral.Id], ignoreOrder: true);
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 5 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 5 });
        fight.Losses[Neutral.Id].ShouldBe(new Army() { Dot = 3 });
        result.State.NeutralArmies.ShouldBeEmpty();
    }

    [Test]
    public void NeutralArmy_StaysWhereItIsWhileNobodyEnters()
    {
        GameState state = new(GuardedCenter(new Army() { Dot = 3 }));

        TurnResult result = state.ResolveTurn(_noCommands);

        result.Report.Fights.ShouldBeEmpty();
        result.State.NeutralArmies.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, Army>(_center, new Army() { Dot = 3 }));
    }

    [Test]
    public void FightAgainstGuards_IsReportedOnlyToThePlayerWhoFought()
    {
        GameState state = new(GuardedCenter(new Army() { Dot = 3 }));

        TurnResult result = Resolve(state, ("Player01", _player1Start, _center, new Army() { Dot = 5 }));

        FogOfWar.GetPlayerTurnReport(result.Report, "Player01").Fights.Keys.ShouldBe([_center]);
        FogOfWar.GetPlayerTurnReport(result.Report, "Player02").Fights.ShouldBeEmpty();
    }

    [Test]
    public void VisibleNeutralArmy_ShowsItsUnitCountUnderTheNeutralId()
    {
        // The center is next to both start hexagons and therefore fully visible.
        GameState state = new(GuardedCenter(new Army() { Dot = 3, Square = 1 }));

        PlayerGameState view = FogOfWar.GetPlayerGameState(state, "Player01");

        view.Observations[_center].OpponentUnitCounts.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, int>(Neutral.Id, 4));
        view.PlayerIds.ShouldNotContain(Neutral.Id);
    }

    [Test]
    public void PartiallyVisibleNeutralArmy_ShowsNoUnitCount()
    {
        // The guard stands two hexagons away from Player01: partially visible, units hidden.
        Hexagon guarded = new(1, 0);
        GameSettings settings = TestSettings.Create(Maps.Line(-1, 3)
            .SetItem(_player1Start, Maps.Start("Player01"))
            .SetItem(guarded, Maps.Hexagon().WithGuard(new Army() { Dot = 3 }))
            .SetItem(new Hexagon(3, 0), Maps.Start("Player02")));

        PlayerGameState view = FogOfWar.GetPlayerGameState(new GameState(settings), "Player01");

        view.Observations[guarded].Visibility.ShouldBe(Visibility.PartiallyVisible);
        view.Observations[guarded].OpponentUnitCounts.ShouldBeEmpty();
    }

    [Test]
    public void NeutralArmy_IsNoPlayerInTheStanding()
    {
        GameStanding standing = Standing.Of(new GameState(GuardedCenter(new Army() { Dot = 3 })));

        standing.DefeatedPlayerIds.ShouldBeEmpty();
        standing.Outcome.Value.ShouldBeOfType<GameRunning>();
    }

    [Test]
    public void NeutralArmyOnAStartHexagon_IsRefused()
    {
        GameSettings settings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01").WithGuard(new Army() { Dot = 3 }))
            .SetItem(_player2Start, Maps.Start("Player02")));

        Should.Throw<ArgumentException>(() => new GameState(settings));
    }

    [Test]
    public void PlayerWithTheNeutralId_IsRefused()
    {
        GameSettings settings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start(Neutral.Id))
            .SetItem(_player2Start, Maps.Start("Player02")));

        Should.Throw<ArgumentException>(() => new GameState(settings));
    }
}
