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
            .ShouldHaveSingleItem().Value
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
    public void Fight_Dot_vs_Dot()
    {
        // Assign
        var gameState = new GameState(_gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Dot = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Dot = 3 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands);
        var turn1 = result1.State;

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Dot = 2 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));

        // Fighting reveals the pre-fight composition of the participating armies to each other.
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .RevealedArmies[new Hexagon(0, 0)]["Player02"].ShouldBe(new Army() { Dot = 3 });
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .RevealedArmies[new Hexagon(0, 0)]["Player01"].ShouldBe(new Army() { Dot = 5 });

        // The revelation only lasts for the turn of the fight.
        var result2 = turn1.ResolveTurn(_noCommands);
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player01").RevealedArmies.ShouldBeEmpty();
    }

    [Test]
    public void Fight_Circle_vs_Circle()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 5, Dot = 5 }, new Compound() { Plane = 1 }), (new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Circle = 3 })
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 2 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_Circle_vs_Square()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 }), (new Army() { Square = 5, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 3 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 1 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_Army_vs_Army()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }, new Compound() { Plane = 1 }), (new Army() { Square = 15, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 15, Dot = 5 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands, validateCommands: false);
        var turn1 = result1.State;

        // Assert
        turn1.PlayerStates["Player02"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Square = 1 });
        turn1.PlayerStates["Player01"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .Losses[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 });
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .Losses[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Dot = 5, Square = 14 });
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
            .Constructions.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().Value.ShouldBe(new Compound() { Pyramid = 2 });
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
        player.Trainings.ShouldHaveSingleItem().Value[_player1Start].ShouldBe(new Army() { Square = 1, Triangle = 1 });
        player.UsedSpace.ShouldBe(6);
    }

    [Test]
    public void Fight_Army_vs_Army_InTwoTurns()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Square = 15 }, new Compound() { Plane = 1 }), (new Army() { Square = 15 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Square = 15 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands, validateCommands: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result2 = result1.State.ResolveTurn(commands, validateCommands: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result3 = result2.State.ResolveTurn(commands, validateCommands: false);

        // Assert
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player02")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player01")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result3.Report, "Player02")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result3.Report, "Player01")
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
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

    [Test]
    public void ExponentBonusFromUpgradesAppliesResearchedLevels()
    {
        var gameState = new GameState(_gameSettings);
        var playerState = gameState.PlayerStates["Player01"];

        playerState.ExponentBonusFromUpgrades(_gameSettings).ShouldBe(Army.Empty);

        var levelOne = playerState with
        {
            UpgradeLevels = new Dictionary<Upgrade, byte>() { { Upgrade.DotUpgrade, 1 } }.ToImmutableDictionary()
        };
        levelOne.ExponentBonusFromUpgrades(_gameSettings).ShouldBe(new Army() { Dot = 20 });

        // The maximum level must use the last defined upgrade instead of indexing past the array.
        var maxLevel = playerState with
        {
            UpgradeLevels = new Dictionary<Upgrade, byte>() { { Upgrade.DotUpgrade, 2 } }.ToImmutableDictionary()
        };
        maxLevel.ExponentBonusFromUpgrades(_gameSettings).ShouldBe(new Army() { Dot = 20 });
    }
}
