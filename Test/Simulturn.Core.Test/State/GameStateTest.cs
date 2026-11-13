using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public sealed class GameStateTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A disc of 7 hexagons. The players start on opposite sides of the center with 5 dots and a plane each.
    /// Only the start hexagons hold matter; the center offers the dot upgrade.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Disc(1)
        .SetItem(_player1Start, Maps.Start("Player01", matter: 10000))
        .SetItem(_player2Start, Maps.Start("Player02", matter: 10000))
        .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.DotUpgrade)));

    /// <summary>
    /// The standard map with different start armies and compounds.
    /// </summary>
    private static GameSettings WithStarts((Army Army, Compound Compound) player1, (Army Army, Compound Compound) player2)
    {
        return TestSettings.Create(_gameSettings.HexagonSettings.ToImmutableDictionary()
            .SetItem(_player1Start, Maps.Start("Player01", matter: 10000, army: player1.Army, compound: player1.Compound))
            .SetItem(_player2Start, Maps.Start("Player02", matter: 10000, army: player2.Army, compound: player2.Compound)));
    }

    [Test]
    public void Initialize()
    {
        var gameState = new GameState(_gameSettings);
        StateValidator.Validate(gameState).ShouldBeEmpty();
        gameState.Turn.ShouldBe((ushort)0);
        gameState.Hexagons.Count.ShouldBe(7);
        gameState.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        gameState.PlayerStates["Player01"].Constructions.ShouldBeEmpty();
        gameState.PlayerStates["Player02"].Constructions.ShouldBeEmpty();
        gameState.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
        gameState.PlayerStates["Player02"].Trainings.ShouldBeEmpty();
        gameState.GameSettings.ShouldBe(_gameSettings);
        gameState.PlayerStates["Player01"].Armies[_player1Start].ShouldBe(new Army() { Dot = 5 });
        gameState.PlayerStates["Player01"].Armies.Count.ShouldBe(1);
        gameState.PlayerStates["Player02"].Armies[_player2Start].ShouldBe(new Army() { Dot = 5 });
        gameState.PlayerStates["Player02"].Armies.Count.ShouldBe(1);
        gameState.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        gameState.PlayerStates["Player01"].Compounds.Count.ShouldBe(1);
        gameState.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1 });
        gameState.PlayerStates["Player02"].Compounds.Count.ShouldBe(1);
        gameState.RemainingMatter.Sum(x => x.Value).ShouldBe(20000);
        gameState.RemainingMatter[_player1Start].ShouldBe(10000);
        gameState.RemainingMatter[_player2Start].ShouldBe(10000);
        gameState.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        gameState.PlayerStates["Player01"].AvailableSpace.ShouldBe(10);
        gameState.PlayerStates["Player01"].UsedSpace.ShouldBe(5);
        gameState.PlayerStates["Player01"].Matter.ShouldBe(500);
        gameState.PlayerStates["Player02"].AvailableSpace.ShouldBe(10);
        gameState.PlayerStates["Player02"].UsedSpace.ShouldBe(5);
        gameState.PlayerStates["Player02"].Matter.ShouldBe(500);

        var newTurn = gameState.PlayTurn(_noCommands);
        newTurn.Turn.ShouldBe((ushort)1);
    }

    [Test]
    public void EmptyTurn()
    {
        var gameState = new GameState(_gameSettings);

        var newTurn = gameState.PlayTurn(_noCommands);
        newTurn.Turn.ShouldBe((ushort)1);
        newTurn.Hexagons.Count.ShouldBe(7);
        newTurn.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        newTurn.PlayerStates["Player01"].Constructions.ShouldBeEmpty();
        newTurn.PlayerStates["Player02"].Constructions.ShouldBeEmpty();
        newTurn.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
        newTurn.PlayerStates["Player02"].Trainings.ShouldBeEmpty();
        newTurn.GameSettings.ShouldBe(_gameSettings);
        newTurn.PlayerStates["Player01"].Armies[_player1Start].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerStates["Player01"].Armies.Count.ShouldBe(1);
        newTurn.PlayerStates["Player02"].Armies[_player2Start].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerStates["Player02"].Armies.Count.ShouldBe(1);
        newTurn.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerStates["Player01"].Compounds.Count.ShouldBe(1);
        newTurn.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerStates["Player02"].Compounds.Count.ShouldBe(1);
        newTurn.RemainingMatter.Sum(x => x.Value).ShouldBe(20000 - 2 * 50);
        newTurn.RemainingMatter[_player1Start].ShouldBe(10000 - 50);
        newTurn.RemainingMatter[_player2Start].ShouldBe(10000 - 50);
        newTurn.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        newTurn.PlayerStates["Player01"].AvailableSpace.ShouldBe(10);
        newTurn.PlayerStates["Player01"].UsedSpace.ShouldBe(5);
        newTurn.PlayerStates["Player01"].Matter.ShouldBe(500 + 50);
        newTurn.PlayerStates["Player02"].AvailableSpace.ShouldBe(10);
        newTurn.PlayerStates["Player02"].UsedSpace.ShouldBe(5);
        newTurn.PlayerStates["Player02"].Matter.ShouldBe(500 + 50);
    }

    [Test]
    public void TrainDots()
    {
        var gameState = new GameState(_gameSettings);
        var dict = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
            {
                "Player01", Command.Create([(new Hexagon(-1,0), new Command() { Training = new Army() { Dot = 1 } })])
            },
        };
        var turn1 = gameState.PlayTurn(dict);
        turn1.Turn.ShouldBe((ushort)1);
        turn1.Hexagons.Count.ShouldBe(7);
        turn1.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        turn1.PlayerStates["Player01"].Constructions.ShouldBeEmpty();
        turn1.PlayerStates["Player02"].Constructions.ShouldBeEmpty();
        turn1.PlayerStates["Player01"].Trainings.ShouldBeEmpty(); // A dot takes one turn, so it is applied and removed in the same turn.
        turn1.PlayerStates["Player02"].Trainings.ShouldBeEmpty();
        turn1.GameSettings.ShouldBe(_gameSettings);
        turn1.PlayerStates["Player01"].Armies[_player1Start].ShouldBe(new Army() { Dot = 6 });
        turn1.PlayerStates["Player01"].Armies.Count.ShouldBe(1);
        turn1.PlayerStates["Player02"].Armies[_player2Start].ShouldBe(new Army() { Dot = 5 });
        turn1.PlayerStates["Player02"].Armies.Count.ShouldBe(1);
        turn1.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        turn1.PlayerStates["Player01"].Compounds.Count.ShouldBe(1);
        turn1.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1 });
        turn1.PlayerStates["Player02"].Compounds.Count.ShouldBe(1);
        turn1.RemainingMatter.Sum(x => x.Value).ShouldBe(20000 - 2 * 50);
        turn1.RemainingMatter[_player1Start].ShouldBe(10000 - 50);
        turn1.RemainingMatter[_player2Start].ShouldBe(10000 - 50);
        turn1.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        turn1.PlayerStates["Player01"].AvailableSpace.ShouldBe(10);
        turn1.PlayerStates["Player01"].UsedSpace.ShouldBe(6);
        turn1.PlayerStates["Player01"].Matter.ShouldBe(500 + 50 - 75);
        turn1.PlayerStates["Player02"].AvailableSpace.ShouldBe(10);
        turn1.PlayerStates["Player02"].UsedSpace.ShouldBe(5);
        turn1.PlayerStates["Player02"].Matter.ShouldBe(500 + 50);
    }

    [Test]
    public void ConstructPyramid()
    {
        var gameState = new GameState(_gameSettings);
        var dict = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
            {
                "Player01", Command.Create([(new Hexagon(-1,0), new Command() { Construction = new Compound() { Pyramid = 1 } })])
            },
        };
        var turn1 = gameState.PlayTurn(dict);
        turn1.Turn.ShouldBe((ushort)1);
        turn1.Hexagons.Count.ShouldBe(7);
        turn1.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        turn1.PlayerStates["Player01"].Trainings.ShouldBeEmpty();
        turn1.PlayerStates["Player02"].Trainings.ShouldBeEmpty();
        turn1.PlayerStates["Player01"].Constructions
            .ShouldHaveSingleItem().Value.ShouldBe(new Compound() { Pyramid = 1 });
        turn1.PlayerStates["Player02"].Constructions.ShouldBeEmpty();
        turn1.GameSettings.ShouldBe(_gameSettings);
        turn1.PlayerStates["Player01"].Armies[_player1Start].ShouldBe(new Army() { Dot = 5 });
        turn1.PlayerStates["Player01"].Armies.Count.ShouldBe(1);
        turn1.PlayerStates["Player02"].Armies[_player2Start].ShouldBe(new Army() { Dot = 5 });
        turn1.PlayerStates["Player02"].Armies.Count.ShouldBe(1);
        turn1.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        turn1.PlayerStates["Player01"].Compounds.Count.ShouldBe(1);
        turn1.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1 });
        turn1.PlayerStates["Player02"].Compounds.Count.ShouldBe(1);
        turn1.RemainingMatter.Sum(x => x.Value).ShouldBe(20000 - 2 * 50 + 10); // 10 less, because one dot is working on the construction
        turn1.RemainingMatter[_player1Start].ShouldBe(10000 - 50 + 10);
        turn1.RemainingMatter[_player2Start].ShouldBe(10000 - 50);
        turn1.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        turn1.PlayerStates["Player01"].AvailableSpace.ShouldBe(10);
        turn1.PlayerStates["Player01"].UsedSpace.ShouldBe(5);
        turn1.PlayerStates["Player01"].Matter.ShouldBe(500 + 40 - 150);
        turn1.PlayerStates["Player02"].AvailableSpace.ShouldBe(10);
        turn1.PlayerStates["Player02"].UsedSpace.ShouldBe(5);
        turn1.PlayerStates["Player02"].Matter.ShouldBe(500 + 50);

        var turn2 = turn1.PlayTurn(_noCommands);
        turn2.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        var turn3 = turn2.PlayTurn(_noCommands);
        turn3.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1, Pyramid = 1 });
    }

    [Test]
    public void UpgradeTest()
    {
        var gameState = new GameState(_gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Dot = 1 })
        ]);
        var turn1 = gameState.PlayTurn(commands);
        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", new Hexagon(0,0),new Command(){ Research = Upgrade.DotUpgrade})
        ]);
        var turn2 = turn1.PlayTurn(commands);
        turn2.PlayerStates["Player01"]
            .Researches.ShouldHaveSingleItem().Value.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        turn2.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        var turn3 = turn2.PlayTurn(_noCommands);
        turn3.PlayerStates["Player01"]
            .Researches.ShouldHaveSingleItem().Value.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        turn3.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        var turn4 = turn3.PlayTurn(_noCommands);
        turn4.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        turn4.PlayerStates["Player01"].UpgradeLevels.ShouldHaveSingleItem().Key.ShouldBe(Upgrade.DotUpgrade);
        turn4.PlayerStates["Player01"].UpgradeLevels.ShouldHaveSingleItem().Value.ShouldBe((byte)1);
    }

    [Test]
    public void CancelConstructionsAfterDotKill()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Dot = 15 }, new Compound() { Plane = 10 }), (new Army() { Square = 3 }, new Compound() { Plane = 10 })) with { StartMatter = 2000 };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Construction = new Compound() { Plane = 2, Pyramid = 2} }),
            ("Player02", _player2Start, new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 3}, Destination = new Hexagon(0,0) }] }),
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Construction = new Compound() { Plane = 1, Dome = 1} }),
            ("Player02", new Hexagon(0,0), new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 3}, Destination = _player1Start }] }),
        ]);

        var turn2 = turn1.PlayTurn(commands);

        // Assert
        turn2.PlayerStates["Player01"]
            .Constructions.ShouldHaveSingleItem().Value.ShouldBe(new Compound() { Pyramid = 2 });
    }

    /// <summary>
    /// 10 squares destroy both domes on turn 1. That cancels the pending circle training of turn 1,
    /// but not the circle that completed on turn 1 and already died in the fight.
    /// </summary>
    [Test]
    public void CancelTrainingsAfterCompoundDestruction()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Dot = 0 }, new Compound() { Pyramid = 2, Cube = 2, Dome = 2, Plane = 2 }), (new Army() { Square = 10 }, new Compound() { Plane = 10 })) with { StartMatter = 20000 };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){Circle = 1, Square = 1, Triangle = 1} }),
            ("Player02", _player2Start, new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = new Hexagon(0,0) }] }),
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){Circle = 1, Square = 1, Triangle = 1} }),
            ("Player02", new Hexagon(0,0), new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = _player1Start }] }),
        ]);

        var turn2 = turn1.PlayTurn(commands);

        // Assert
        var player = turn2.PlayerStates["Player01"];
        player.Compounds[_player1Start].ShouldBe(new Compound() { Pyramid = 2, Cube = 2, Plane = 2 });
        player.Armies.GetValueOrDefault(_player1Start).ShouldBe(Army.Empty);
        player.Trainings.ShouldHaveSingleItem().Value.ShouldBe(new Army() { Square = 1, Triangle = 1 });
        player.UsedSpace.ShouldBe(6);
    }

    /// <summary>
    /// As above, but only circles are trained: the canceled training was the only pending one,
    /// so nothing is left in the queue, not even an empty entry.
    /// </summary>
    [Test]
    public void CancelTheOnlyPendingTraining_LeavesTheQueueEmpty()
    {
        // Arrange
        var gameSettings = WithStarts((new Army() { Dot = 0 }, new Compound() { Pyramid = 2, Cube = 2, Dome = 2, Plane = 2 }), (new Army() { Square = 10 }, new Compound() { Plane = 10 })) with { StartMatter = 20000 };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){ Circle = 1 } }),
            ("Player02", _player2Start, new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = new Hexagon(0,0) }] }),
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){ Circle = 1 } }),
            ("Player02", new Hexagon(0,0), new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = _player1Start }] }),
        ]);

        var turn2 = turn1.PlayTurn(commands);

        // Assert
        var player = turn2.PlayerStates["Player01"];
        player.Compounds[_player1Start].ShouldBe(new Compound() { Pyramid = 2, Cube = 2, Plane = 2 });
        player.Trainings.ShouldBeEmpty();
        player.UsedSpace.ShouldBe(0);
    }

    [Test]
    public void IncomeIsCappedByMaxNumberOfUnitsGeneratingMatter()
    {
        var gameSettings = TestSettings.Create(_gameSettings.HexagonSettings.ToImmutableDictionary()
            .SetItem(_player1Start, Maps.Start("Player01", matter: 10000, army: new Army() { Dot = 15 }) with
            {
                MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 3 }
            }));

        var gameState = new GameState(gameSettings);
        var turn1 = gameState.PlayTurn(_noCommands);

        turn1.PlayerStates["Player01"].Matter.ShouldBe(gameSettings.StartMatter + 30);
        turn1.RemainingMatter[_player1Start].ShouldBe(10000 - 30);
    }

    [Test]
    public void ValidateReturnsInsufficientMatterWhenCombinedCostsExceedBudget()
    {
        var gameSettings = _gameSettings with
        {
            StartMatter = 200
        };
        var gameState = new GameState(gameSettings);

        Dictionary<string, Dictionary<Hexagon, Command>> commands =
            new Dictionary<string, Dictionary<Hexagon, Command>>()
            {
                {
                    "Player01",
                    Command.Create(
                    [
                        (
                            _player1Start,
                            new Command()
                            {
                                Training = new Army() { Dot = 1 },
                                Construction = new Compound() { Pyramid = 1 }
                            }
                        )
                    ])
                }
            };

        var validations = CommandValidator.Validate(gameState, "Player01", commands["Player01"]);

        validations.Select(x => x.Value).OfType<InsufficientMatter>().ShouldHaveSingleItem();
    }

    [Test]
    public void NextTurnThrowsWhenTurnLimitIsReached()
    {
        var gameState = new GameState(_gameSettings) with { Turn = ushort.MaxValue };

        Should.Throw<InvalidOperationException>(() => TurnResolution.Resolve(gameState, _noCommands));
    }

    [Test]
    public void Train_NoSupply()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Dot = 9 }, new Compound() { Plane = 1, Cube = 1, Dome = 1, Pyramid = 1 }), (new Army() { Square = 10 }, new Compound() { Plane = 1 })) with { StartMatter = 1000 };
        var gameState = new GameState(gameSettings);

        var commands = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
            {
                "Player01", Command.Create([(new Hexagon(-1,0), new Command() { Training = new Army() { Circle = 1, Square = 1, Triangle = 1 } })])
            },
        };

        // Act
        var validations = CommandValidator.Validate(gameState, commands.Keys.Single(), commands.Single().Value);
        validations.ShouldHaveSingleItem().Value.ShouldBeOfType<InsufficientSpace>().Required.ShouldBe(9 + 9);
    }
}
