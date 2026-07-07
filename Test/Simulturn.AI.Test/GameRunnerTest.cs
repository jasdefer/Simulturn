using Simulturn.AI.Players;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.AI.Test;

public class GameRunnerTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends.
    /// </summary>
    private static GameSettings CreateSettings(Army player1Army, Army player2Army)
    {
        return new GameSettings()
        {
            Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
            ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
            CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
            ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
            FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
            Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
            ProvidedSpace = new Compound() { Axis = 10, Dome = 0, Cube = 0, Plane = 10, Pyramid = 0 },
            RequiredSpace = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 1 },
            Seed = 1,
            StartMatter = 500,
            PartialVisibilityRange = 2,
            VisibilityRange = 1,
            StructureDamage = new Army() { Triangle = 5, Circle = 5, Square = 5, Dot = 1 },
            TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
            StartUpgrades = ImmutableDictionary<string, ImmutableArray<Upgrade>>.Empty,
            Upgrades = new IUpgrade[]
                {
                    new DotUpgrade() { Cost = 150, Duration = 3, ExponentBonus = 20 },
                    new DotUpgrade() { Cost = 175, Duration = 4, ExponentBonus = 20 }
                }.ToDictionary(),
            HexagonSettings = new Dictionary<Hexagon, HexagonSettings>
            {
                {
                    _player1Start, new HexagonSettings()
                    {
                        Matter = 1000,
                        MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                        PlayerInitialization = ("Player01", player1Army, new Compound() { Plane = 1 })
                    }
                },
                {
                    new Hexagon(0, 0), HexagonSettings.Empty
                },
                {
                    _player2Start, new HexagonSettings()
                    {
                        Matter = 1000,
                        MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                        PlayerInitialization = ("Player02", player2Army, new Compound() { Plane = 1 })
                    }
                }
            }.ToImmutableDictionary()
        };
    }

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
        var gameSettings = CreateSettings(new Army() { Square = 20 }, new Army() { Dot = 5 });
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
        var gameSettings = CreateSettings(new Army() { Square = 20 }, new Army() { Square = 20 });
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
