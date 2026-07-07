using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Web.Board;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

/// <summary>
/// The board projection must faithfully map the engine's fog-of-war view (PlayerGameState)
/// and never add information of its own.
/// </summary>
public class FogProjectionTest
{
    private static readonly string[] _players = ["Player 1", "Player 2"];

    // Default settings: VisibilityRange 1, PartialVisibilityRange 2, starts far apart (radius 4 map).
    private static GameState NewState() => new(DefaultSettings.Standard(_players, seed: 1));

    private static Hexagon Start(GameState state, string playerId) =>
        state.PlayerStates[playerId].Compounds.Keys.Single();

    [Test]
    public void PlayerView_HidesDistantEnemies()
    {
        var state = NewState();
        var board = new BoardProjector().ProjectPlayerView(state.GetPlayerGameState("Player 1"));

        var enemyStart = Start(state, "Player 2");
        var enemyHex = board.Hexes.Single(hex => hex.Hex == enemyStart);
        // Enemy start is far outside all vision ranges: nothing may be revealed.
        enemyHex.Occupants.ShouldBeEmpty();
        enemyHex.RemainingMatter.ShouldBeNull(); // never seen up close → only initial matter is known
    }

    [Test]
    public void PlayerView_NeverContainsEnemyGhostsOrQueues()
    {
        var state = NewState();

        // Both players stage orders; the enemy's must not appear in Player 1's view.
        var drafts = new Dictionary<string, Dictionary<Hexagon, DraftCommand>>();
        foreach (var playerId in _players)
        {
            var draft = new DraftCommand();
            draft.Training.Dot = 2;
            draft.UpsertMovement(new Hexagon(0, 0), new ArmyDraft { Dot = 1 });
            drafts[playerId] = new Dictionary<Hexagon, DraftCommand> { [Start(state, playerId)] = draft };
        }

        var board = new BoardProjector().ProjectPlayerView(state.GetPlayerGameState("Player 1"), drafts);

        var ownIndex = board.PlayerIndexById["Player 1"];
        board.Arrows.ShouldAllBe(arrow => arrow.PlayerIndex == ownIndex);
        foreach (var occupant in board.Hexes.SelectMany(hex => hex.Occupants).Where(occupant => occupant.PlayerId != "Player 1"))
        {
            occupant.GhostTraining.IsEmpty.ShouldBeTrue();
            occupant.GhostConstruction.IsEmpty.ShouldBeTrue();
            occupant.GhostInboundArmy.IsEmpty.ShouldBeTrue();
            occupant.InProgress.ShouldBeEmpty();
        }
    }

    [Test]
    public void PlayerView_AdjacentEnemy_ShowsCountNeverComposition()
    {
        var state = NewState();
        var ownStart = Start(state, "Player 1");
        var enemyStart = Start(state, "Player 2");

        // Move an enemy army right next to Player 1's start (distance 1 = fully Visible).
        var neighbor = state.Hexagons
            .First(hexagon => hexagon.DistanceTo(ownStart) == 1 && hexagon != ownStart);
        var commands = new Dictionary<string, Dictionary<Hexagon, Command>>
        {
            ["Player 2"] = new()
            {
                [enemyStart] = new Command
                {
                    MovementCommands = [new MovementCommand { Destination = neighbor, Army = new Army { Dot = 3 } }],
                },
            },
        };
        var nextState = state.NextTurn(commands);

        var board = new BoardProjector().ProjectPlayerView(nextState.GetPlayerGameState("Player 1"));
        var hex = board.Hexes.Single(view => view.Hex == neighbor);
        hex.Visibility.ShouldBe(Visibility.Visible);
        var enemy = hex.Occupants.ShouldHaveSingleItem();
        enemy.Army.ShouldBeNull();          // composition is never revealed ("until infiltration exists")
        enemy.ArmyCountOnly.ShouldBe(3);    // count visible at full visibility
    }

    [Test]
    public void PlayerView_RemembersEnemyBuildings_AsStale()
    {
        var state = NewState();
        var ownStart = Start(state, "Player 1");
        var enemyStart = Start(state, "Player 2");

        // Scout: move one own dot next to the enemy base so its buildings become visible...
        var scoutHexagon = state.Hexagons.First(hexagon => hexagon.DistanceTo(enemyStart) == 1);
        var scoutCommands = new Dictionary<string, Dictionary<Hexagon, Command>>
        {
            ["Player 1"] = new()
            {
                [ownStart] = new Command
                {
                    MovementCommands = [new MovementCommand { Destination = scoutHexagon, Army = new Army { Dot = 1 } }],
                },
            },
        };
        var scouted = state.NextTurn(scoutCommands);
        var seen = new BoardProjector().ProjectPlayerView(scouted.GetPlayerGameState("Player 1"));
        var enemyHexSeen = seen.Hexes.Single(hex => hex.Hex == enemyStart);
        var enemySeen = enemyHexSeen.Occupants.ShouldHaveSingleItem();
        enemySeen.Compound.Plane.ShouldBe((short)1);
        enemySeen.CompoundStale.ShouldBeFalse();

        // ...then retreat: the memory must persist and be marked stale.
        var retreatCommands = new Dictionary<string, Dictionary<Hexagon, Command>>
        {
            ["Player 1"] = new()
            {
                [scoutHexagon] = new Command
                {
                    MovementCommands = [new MovementCommand { Destination = ownStart, Army = new Army { Dot = 1 } }],
                },
            },
        };
        var retreated = scouted.NextTurn(retreatCommands);
        var remembered = new BoardProjector().ProjectPlayerView(retreated.GetPlayerGameState("Player 1"));
        var enemyHexRemembered = remembered.Hexes.Single(hex => hex.Hex == enemyStart);
        var enemyRemembered = enemyHexRemembered.Occupants.ShouldHaveSingleItem();
        enemyRemembered.Compound.Plane.ShouldBe((short)1); // remembered building
        enemyRemembered.CompoundStale.ShouldBeTrue();      // marked as a memory
        enemyRemembered.ArmyCountOnly.ShouldBeNull();      // unit counts are current-visibility only
        enemyHexRemembered.MatterStale.ShouldBeTrue();
        enemyHexRemembered.RemainingMatter.ShouldNotBeNull(); // last-seen snapshot
    }

    [Test]
    public void PlayerView_OwnContent_IsAlwaysFullyVisible()
    {
        var state = NewState();
        var ownStart = Start(state, "Player 1");
        var board = new BoardProjector().ProjectPlayerView(state.GetPlayerGameState("Player 1"));
        var hex = board.Hexes.Single(view => view.Hex == ownStart);
        hex.Visibility.ShouldBe(Visibility.Occupied);
        var own = hex.Occupants.ShouldHaveSingleItem();
        own.Army!.Value.Dot.ShouldBe((short)5);
        own.Compound.Plane.ShouldBe((short)1);
        hex.RemainingMatter.ShouldNotBeNull();
        hex.MatterStale.ShouldBeFalse();
    }

    [Test]
    public void HotSeat_SubmitFlow_HandsOffThenResolves()
    {
        var session = new GameSession();
        session.StartNewGame(DefaultSettings.Standard(_players, seed: 1), GameMode.HotSeat);
        session.ShowHandoff.ShouldBeTrue();
        session.ActivePlayerId.ShouldBe("Player 1");

        session.BeginPlayerTurn();
        session.ShowHandoff.ShouldBeFalse();

        session.SubmitActivePlayer();
        session.ActivePlayerId.ShouldBe("Player 2");
        session.ShowHandoff.ShouldBeTrue();
        session.Current.Turn.ShouldBe((ushort)0); // not resolved yet

        session.BeginPlayerTurn();
        session.SubmitActivePlayer();
        session.Current.Turn.ShouldBe((ushort)1); // everyone submitted → resolved
        session.ActivePlayerId.ShouldBe("Player 1");
        session.ShowHandoff.ShouldBeTrue();
        session.SubmittedPlayers.ShouldBeEmpty();
    }
}
