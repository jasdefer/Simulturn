using Simulturn.Core.Engine;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

public sealed class MemoryTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-3, 0);
    private static readonly Hexagon _player2Start = new Hexagon(3, 0);

    /// <summary>
    /// A map of 7 hexagons in a line from (-3,0) to (3,0), each holding 1000 matter.
    /// The players start on the opposite ends with 5 dots and a plane each. With a visibility
    /// range of 1 and a partial visibility range of 2, a scout on (1,0) sees the buildings of
    /// the opponent start hexagon, a scout on (2,0) also its units.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-3, 3, matter: 1000)
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02")));

    /// <summary>
    /// Moves a single dot of the first player along the line, one hexagon per turn, and returns
    /// the views of the first player: the start of the game and one view per turn.
    /// </summary>
    private static List<PlayerGameState> Scout(params short[] path)
    {
        return Scout(new GameState(_gameSettings), _player1Start, path).Views;
    }

    private static (List<PlayerGameState> Views, GameState State) Scout(GameState state, Hexagon from, params short[] path)
    {
        List<PlayerGameState> views = [FogOfWar.GetPlayerGameState(state, "Player01")];
        foreach (short x in path)
        {
            Hexagon to = new(x, 0);
            state = state.PlayTurn(DictionaryExtensions.MovementsToDictionary([("Player01", from, to, new Army() { Dot = 1 })]));
            views.Add(FogOfWar.GetPlayerGameState(state, "Player01"));
            from = to;
        }
        return (views, state);
    }

    [Test]
    public void FirstView_RemembersWhatIsInSight()
    {
        var views = Scout();

        var memories = FogOfWar.GetMemories(views);

        // The occupied start hexagon: matter, buildings and units are seen.
        HexagonMemory start = memories[_player1Start];
        start.Static.Turn.ShouldBe((ushort)0);
        start.Static.RemainingMatter.ShouldBe(1000);
        start.Units.ShouldNotBeNull();
        start.Units.Turn.ShouldBe((ushort)0);
        start.Units.OpponentUnitCounts.ShouldBeEmpty();

        // Two hexagons away: partially visible, so matter and buildings but no units.
        memories[new Hexagon(-1, 0)].Static.Turn.ShouldBe((ushort)0);
        memories[new Hexagon(-1, 0)].Units.ShouldBeNull();
    }

    [Test]
    public void NeverSeenHexagon_HasNoMemory()
    {
        var views = Scout(-2, -1);

        FogOfWar.GetMemories(views).ShouldNotContainKey(_player2Start);
    }

    [Test]
    public void HexagonInSight_CarriesTheCurrentTurn()
    {
        // The start hexagon stays occupied by the remaining four dots.
        var views = Scout(-2, -1);

        HexagonMemory memory = FogOfWar.GetMemories(views)[_player1Start];

        memory.Static.Turn.ShouldBe((ushort)2);
        ((int?)memory.Static.RemainingMatter).ShouldBe(views[^1].Observations[_player1Start].RemainingMatter);
        memory.Units.ShouldNotBeNull();
        memory.Units.Turn.ShouldBe((ushort)2);
    }

    [Test]
    public void HexagonOutOfSight_KeepsTheLastSightingOfMatterAndBuildings()
    {
        // On turn 4 the scout stands on (1,0) and sees the opponent start hexagon partially.
        // On turn 5 it is back on (0,0), out of sight.
        var views = Scout(-2, -1, 0, 1, 0);

        HexagonMemory memory = FogOfWar.GetMemories(views)[_player2Start];

        memory.Static.Turn.ShouldBe((ushort)4);
        memory.Static.RemainingMatter.ShouldBe(1000 - 4 * 50); // 5 dots harvest 50 matter per turn
        memory.Static.OpponentCompounds.ShouldHaveSingleItem();
        memory.Static.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });
        memory.Units.ShouldBeNull(); // the hexagon was never fully visible
    }

    [Test]
    public void Memory_GetsStaleWhileOutOfSightAndIsRenewedBySeeingAgain()
    {
        var (views, state) = Scout(new GameState(_gameSettings), _player1Start, -2, -1, 0, 1, 0);

        // The opponent constructs an axis while unseen.
        state = state.PlayTurn(DictionaryExtensions.CommandsToDictionary([
            ("Player02", _player2Start, new Command() { Construction = new Compound() { Axis = 1 } })
        ]));
        views.Add(FogOfWar.GetPlayerGameState(state, "Player01"));
        state = state.PlayEmptyTurn();
        views.Add(FogOfWar.GetPlayerGameState(state, "Player01"));
        state.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1, Axis = 1 });

        StaticSighting sighting = FogOfWar.GetMemories(views)[_player2Start].Static;
        sighting.Turn.ShouldBe((ushort)4);
        sighting.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });

        // Scout again: the memory takes over what the view shows.
        var (rescoutViews, _) = Scout(state, new Hexagon(0, 0), 1);
        views.Add(rescoutViews[^1]);

        sighting = FogOfWar.GetMemories(views)[_player2Start].Static;
        sighting.Turn.ShouldBe((ushort)8);
        sighting.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1, Axis = 1 });
    }

    [Test]
    public void Units_AreRememberedFromTheLastTurnOfFullVisibility()
    {
        // Turn 5: the scout on (2,0) sees the units. Turn 6: back on (1,0), only buildings are visible.
        var views = Scout(-2, -1, 0, 1, 2, 1);

        HexagonMemory memory = FogOfWar.GetMemories(views)[_player2Start];

        memory.Units.ShouldNotBeNull();
        memory.Units.Turn.ShouldBe((ushort)5);
        memory.Units.OpponentUnitCounts.ShouldHaveSingleItem();
        memory.Units.OpponentUnitCounts["Player02"].ShouldBe(5);
        memory.Static.Turn.ShouldBe((ushort)6); // matter and buildings are still in sight
    }

    [Test]
    public void MatterAndUnits_CanStemFromDifferentEarlierTurns()
    {
        // Units last seen on turn 5 from (2,0), buildings last seen on turn 6 from (1,0),
        // on turn 7 the scout is back on (0,0).
        var views = Scout(-2, -1, 0, 1, 2, 1, 0);

        HexagonMemory memory = FogOfWar.GetMemories(views)[_player2Start];

        memory.Static.Turn.ShouldBe((ushort)6);
        memory.Static.RemainingMatter.ShouldBe(1000 - 6 * 50);
        memory.Units.ShouldNotBeNull();
        memory.Units.Turn.ShouldBe((ushort)5);
        memory.Units.OpponentUnitCounts["Player02"].ShouldBe(5);
    }

    [Test]
    public void HexagonSeenWithoutUnits_IsRememberedAsEmpty()
    {
        // On turn 4 the scout on (1,0) sees (2,0) fully and empty. On turn 5 it is back on (0,0).
        var views = Scout(-2, -1, 0, 1, 0);

        HexagonMemory memory = FogOfWar.GetMemories(views)[new Hexagon(2, 0)];

        memory.Units.ShouldNotBeNull();
        memory.Units.Turn.ShouldBe((ushort)4);
        memory.Units.OpponentUnitCounts.ShouldBeEmpty();
    }

    [Test]
    public void UpdateMemories_TakesOverTheViewAndKeepsTheRest()
    {
        var views = Scout(-2, -1, 0, 1, 0);
        var memories = FogOfWar.GetMemories(views[..5]); // up to turn 4, the opponent start hexagon in sight

        memories = FogOfWar.UpdateMemories(memories, views[5]);

        memories[new Hexagon(0, 0)].Static.Turn.ShouldBe((ushort)5); // the scout stands here on turn 5
        memories[_player2Start].Static.Turn.ShouldBe((ushort)4); // out of sight on turn 5
        memories[_player2Start].Static.RemainingMatter.ShouldBe(1000 - 4 * 50);
    }

    [Test]
    public void UpdateMemories_LeavesTheGivenMemoriesUnchanged()
    {
        var views = Scout(-2);
        var memories = FogOfWar.GetMemories(views[..1]);

        FogOfWar.UpdateMemories(memories, views[1]);

        memories[_player1Start].Static.Turn.ShouldBe((ushort)0);
    }

    [Test]
    public void MissingView_IsATurnWithoutKnowledge()
    {
        var views = Scout(-2, -1, 0, 1, 0);
        views.RemoveAt(4); // the only turn the opponent start hexagon was seen

        FogOfWar.GetMemories(views).ShouldNotContainKey(_player2Start);
    }

    [Test]
    public void GetMemories_IsEmptyWithoutViews()
    {
        FogOfWar.GetMemories([]).ShouldBeEmpty();
    }

    [Test]
    public void GetMemories_ThrowsForViewsOutOfTurnOrder()
    {
        var views = Scout(-2, -1);
        views.Reverse();

        Should.Throw<ArgumentException>(() => FogOfWar.GetMemories(views));
    }

    [Test]
    public void GetMemories_ThrowsForViewsOfDifferentPlayers()
    {
        var state = new GameState(_gameSettings);
        List<PlayerGameState> views =
        [
            FogOfWar.GetPlayerGameState(state, "Player01"),
            FogOfWar.GetPlayerGameState(state.PlayEmptyTurn(), "Player02")
        ];

        Should.Throw<ArgumentException>(() => FogOfWar.GetMemories(views));
    }
}
