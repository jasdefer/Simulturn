using Simulturn.AI.Players;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.AI.Test;

public sealed class GameRunnerTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends.
    /// </summary>
    private static GameSettings CreateSettings(Army player1Army, Army player2Army)
    {
        return TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01", army: player1Army))
            .SetItem(_player2Start, Maps.Start("Player02", army: player2Army)));
    }

    /// <summary>
    /// Squares that cross the whole line in one turn, for the tests that attack the opposite start directly.
    /// </summary>
    private static readonly Army _rangeAcrossTheLine = new() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 };

    private static Dictionary<Hexagon, Command> MoveAll(Hexagon from, Hexagon to, Army army)
    {
        return new Dictionary<Hexagon, Command>()
        {
            {
                from, new Command()
                {
                    MovementCommands = [new MovementCommand() { Army = army, Destination = to }]
                }
            }
        };
    }

    [Test]
    public void TwoIdlePlayers_EndInTurnLimitDraw()
    {
        var gameSettings = CreateSettings(new Army() { Dot = 5 }, new Army() { Dot = 5 });
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new IdlePlayer() },
            { "Player02", new IdlePlayer() }
        };
        int completedTurns = 0;

        var result = GameRunner.Run(gameSettings, players, maxTurns: 10, onTurnCompleted: _ => completedTurns++);

        result.EndReason.ShouldBe(GameEndReason.TurnLimit);
        result.WinnerId.ShouldBeNull();
        result.Turns.ShouldBe((ushort)10);
        completedTurns.ShouldBe(10);
        result.FinalState.IsGameOver.ShouldBeFalse();
    }

    [Test]
    public void AggressivePlayer_WinsByDestroyingAllBuildings()
    {
        var gameSettings = CreateSettings(new Army() { Square = 20 }, new Army() { Dot = 5 }) with { MovementRange = _rangeAcrossTheLine };
        var attacker = new ScriptedPlayer(view => view.Turn == 0
            ? MoveAll(_player1Start, _player2Start, new Army() { Square = 20 })
            : new Dictionary<Hexagon, Command>());
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", attacker },
            { "Player02", new IdlePlayer() }
        };

        var result = GameRunner.Run(gameSettings, players);

        result.EndReason.ShouldBe(GameEndReason.Victory);
        result.WinnerId.ShouldBe("Player01");
        result.Turns.ShouldBe((ushort)1);
        result.FinalState.PlayerStates["Player02"].Compounds.Values.ShouldAllBe(x => x.IsEmpty);
    }

    [Test]
    public void SimultaneousBaseTrade_EndsInMutualDestruction()
    {
        var gameSettings = CreateSettings(new Army() { Square = 20 }, new Army() { Square = 20 }) with { MovementRange = _rangeAcrossTheLine };
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            {
                "Player01", new ScriptedPlayer(view => view.Turn == 0
                    ? MoveAll(_player1Start, _player2Start, new Army() { Square = 20 })
                    : new Dictionary<Hexagon, Command>())
            },
            {
                "Player02", new ScriptedPlayer(view => view.Turn == 0
                    ? MoveAll(_player2Start, _player1Start, new Army() { Square = 20 })
                    : new Dictionary<Hexagon, Command>())
            }
        };

        var result = GameRunner.Run(gameSettings, players);

        result.EndReason.ShouldBe(GameEndReason.MutualDestruction);
        result.WinnerId.ShouldBeNull();
        result.Turns.ShouldBe((ushort)1);
    }

    [Test]
    public void Fight_ReachesThePlayersThroughTheReportOfTheirNextView()
    {
        Hexagon center = new(0, 0);
        var gameSettings = CreateSettings(new Army() { Dot = 5 }, new Army() { Dot = 3 });
        Dictionary<ushort, PlayerGameState> player1Views = [];
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            {
                "Player01", new ScriptedPlayer(view =>
                {
                    player1Views[view.Turn] = view;
                    return view.Turn == 0
                        ? MoveAll(_player1Start, center, new Army() { Dot = 5 })
                        : new Dictionary<Hexagon, Command>();
                })
            },
            {
                "Player02", new ScriptedPlayer(view => view.Turn == 0
                    ? MoveAll(_player2Start, center, new Army() { Dot = 3 })
                    : new Dictionary<Hexagon, Command>())
            }
        };
        List<TurnResult> turns = [];

        GameRunner.Run(gameSettings, players, maxTurns: 3, onTurnCompleted: turns.Add);

        player1Views[0].Report.Fights.ShouldBeEmpty();
        player1Views[1].Report.Fights[center].Armies["Player02"].ShouldBe(new Army() { Dot = 3 });
        player1Views[1].Report.Fights[center].Losses["Player01"].ShouldBe(new Army() { Dot = 3 });
        player1Views[2].Report.Fights.ShouldBeEmpty();
        turns.Select(x => x.Report.Fights.Count).ShouldBe([1, 0, 0]);
    }

    [Test]
    public void Run_ThrowsWhenPlayerReturnsInvalidCommands()
    {
        var gameSettings = CreateSettings(new Army() { Dot = 5 }, new Army() { Dot = 5 });
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            {
                // Training a square requires a cube, which the player does not have.
                "Player01", new ScriptedPlayer(_ => new Dictionary<Hexagon, Command>()
                {
                    { _player1Start, new Command() { Training = new Army() { Square = 1 } } }
                })
            },
            { "Player02", new IdlePlayer() }
        };

        var exception = Should.Throw<InvalidCommandsException>(() => GameRunner.Run(gameSettings, players));
        exception.PlayerId.ShouldBe("Player01");
        exception.Validations.ShouldNotBeEmpty();
    }

    [Test]
    public void Run_ThrowsWhenPlayersDoNotMatchTheSettings()
    {
        var gameSettings = CreateSettings(new Army() { Dot = 5 }, new Army() { Dot = 5 });
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new IdlePlayer() }
        };

        Should.Throw<ArgumentException>(() => GameRunner.Run(gameSettings, players));
    }
}
