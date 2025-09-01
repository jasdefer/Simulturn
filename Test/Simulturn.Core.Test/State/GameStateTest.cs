using Simulturn.Core.Extensions;
using Simulturn.Core.Helper;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;
public class GameStateTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];
    private static readonly GameSettings _gameSettings = new()
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
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player01",new Army(){Dot = 5}, new Compound(){Plane = 1})
                }
            },
            {
                _player2Start, new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player02",new Army(){Dot = 5}, new Compound(){Plane = 1})
                }
            },
            {
                new Hexagon(0,0), HexagonSettings.Empty with { ResearchableUpgrades = [Upgrade.DotUpgrade, Upgrade.DotUpgrade] }
            },
            {
                new Hexagon(0,1), HexagonSettings.Empty
            },
            {
                new Hexagon(0,-1), HexagonSettings.Empty
            },
            {
                new Hexagon(-1,1), HexagonSettings.Empty
            },
            {
                new Hexagon(1,-1), HexagonSettings.Empty
            }
        }.ToImmutableDictionary(),
    };

    private static GameState GetNextTurnAndValidate(GameState gameState, Dictionary<string, Dictionary<Hexagon, Command>> commands, bool valiateCommand = true)
    {
        if (valiateCommand)
        {
            foreach ((string playerId, Dictionary<Hexagon, Command> playerCommands) in commands)
            {
                gameState.Validate(playerId, playerCommands).ShouldBeEmpty();
            }
        }
        var newState = gameState.NextTurn(commands);
        newState.IsValid().ShouldBeEmpty();
        return newState;
    }

    [Test]
    public void Initialize()
    {
        var gameState = new GameState(_gameSettings);
        gameState.IsValid().ShouldBeEmpty();
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

        var newTurn = GetNextTurnAndValidate(gameState, _noCommands);
        newTurn.Turn.ShouldBe((ushort)1);
    }

    [Test]
    public void EmptyTurn()
    {
        var gameState = new GameState(_gameSettings);

        var newTurn = GetNextTurnAndValidate(gameState, _noCommands);
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
        var turn1 = GetNextTurnAndValidate(gameState, dict);
        turn1.Turn.ShouldBe((ushort)1);
        turn1.Hexagons.Count.ShouldBe(7);
        turn1.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        turn1.PlayerStates["Player01"].Constructions.ShouldBeEmpty();
        turn1.PlayerStates["Player02"].Constructions.ShouldBeEmpty();
        turn1.PlayerStates["Player01"].Trainings
            .ShouldHaveSingleItem().Value
            .ShouldHaveSingleItem().Value.ShouldBe(new Army() { Dot = 1 });
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
        var turn1 = GetNextTurnAndValidate(gameState, dict);
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

        var turn2 = GetNextTurnAndValidate(turn1, _noCommands);
        turn2.PlayerStates["Player01"].Compounds[_player1Start].ShouldBe(new Compound() { Plane = 1 });
        var turn3 = GetNextTurnAndValidate(turn2, _noCommands);
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
        var turn1 = GetNextTurnAndValidate(gameState, commands);

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Dot = 2 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_Circle_vs_Circle()
    {
        // Assign
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Circle = 5, Dot = 5 }, new Compound() { Plane = 1 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Circle = 3 })
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands);

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
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 5, Dot = 5 }, new Compound() { Plane = 1 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 3 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands);

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
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }, new Compound() { Plane = 1 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 15, Dot = 5 }, new Compound() { Plane = 1 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 15, Dot = 5 })
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands, valiateCommand: false);

        // Assert
        turn1.PlayerStates["Player02"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Square = 1 });
        turn1.PlayerStates["Player01"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
        turn1.PlayerStates["Player01"]
            .Losses[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 });
        turn1.PlayerStates["Player02"]
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
        var turn1 = GetNextTurnAndValidate(gameState, commands);
        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", new Hexagon(0,0),new Command(){ Upgrade = Upgrade.DotUpgrade})
        ]);
        var turn2 = GetNextTurnAndValidate(turn1, commands);
        turn2.PlayerStates["Player01"]
            .Researches.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().Value.ShouldBe(Upgrade.DotUpgrade);
        turn2.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        var turn3 = GetNextTurnAndValidate(turn2, _noCommands);
        turn3.PlayerStates["Player01"]
            .Researches.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().Value.ShouldBe(Upgrade.DotUpgrade);
        turn3.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        var turn4 = GetNextTurnAndValidate(turn3, _noCommands);
        turn4.PlayerStates["Player01"]
            .Researches.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().Value.ShouldBe(Upgrade.DotUpgrade);
        turn4.PlayerStates["Player01"].UpgradeLevels.ShouldHaveSingleItem().Key.ShouldBe(Upgrade.DotUpgrade);
        turn4.PlayerStates["Player01"].UpgradeLevels.ShouldHaveSingleItem().Value.ShouldBe((byte)1);
    }

    [Test]
    public void CancelConstructionsAfterDotKill()
    {
        // Assign
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Dot = 15 }, new Compound() { Plane = 10 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 3 }, new Compound() { Plane = 10 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Construction = new Compound() { Plane = 2, Pyramid = 2} }),
            ("Player02", _player2Start, new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 3}, Destination = new Hexagon(0,0) }] }),
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands, false);

        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Construction = new Compound() { Plane = 1, Dome = 1} }),
            ("Player02", new Hexagon(0,0), new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 3}, Destination = _player1Start }] }),
        ]);

        var turn2 = GetNextTurnAndValidate(turn1, commands, false);

        // Assert
        turn2.PlayerStates["Player01"]
            .Constructions.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().Value.ShouldBe(new Compound() { Pyramid = 2 });
    }

    [Test]
    public void CancelTrainingsAfterCompoundDestruction()
    {
        // Assign
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Dot = 0 }, new Compound() { Pyramid = 2, Cube = 2, Dome = 2 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 10 }, new Compound() { Plane = 10 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary(),
            StartMatter = 20000
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){Circle = 1, Square = 1, Triangle = 1} }),
            ("Player02", _player2Start, new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = new Hexagon(0,0) }] }),
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands);

        commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command() { Training = new Army(){Circle = 1, Square = 1, Triangle = 1} }),
            ("Player02", new Hexagon(0,0), new Command() { MovementCommands = [ new MovementCommand() { Army = new Army() { Square = 10}, Destination = _player1Start }] }),
        ]);

        var turn2 = GetNextTurnAndValidate(turn1, commands);

        // Assert
        turn2.PlayerStates["Player01"]
            .Trainings.Sum(x => x.Value.Sum(y => y.Value.Total)).ShouldBe(4);
        File.WriteAllText("test.svg", Printer.PrintState(turn2, Printer.GetFullInfo));
    }

    [Test]
    public void Fight_Army_vs_Army_InTwoTurns()
    {
        // Assign
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Square = 15 }, new Compound() { Plane = 1 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 15 }, new Compound() { Plane = 1 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Square = 15 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn1 = GetNextTurnAndValidate(gameState, commands, valiateCommand: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn2 = GetNextTurnAndValidate(turn1, commands, valiateCommand: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn3 = GetNextTurnAndValidate(turn1, commands, valiateCommand: false);

        // Assert
        turn1.PlayerStates["Player02"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        turn1.PlayerStates["Player01"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        turn2.PlayerStates["Player02"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        turn2.PlayerStates["Player01"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        turn3.PlayerStates["Player02"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
        turn3.PlayerStates["Player01"]
            .Losses.Keys.ShouldContain(new Hexagon(0, 0));
    }

    [Test]
    public void Train_NoSupply()
    {
        // Assign
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player1Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player01", new Army() { Dot = 9 }, new Compound() { Plane = 1, Cube = 1, Dome = 1, Pyramid = 1 })
        };
        builder[_player2Start] = new HexagonSettings()
        {
            IsBuildable = true,
            Matter = 10000,
            MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
            PlayerInitialization = new("Player02", new Army() { Square = 10 }, new Compound() { Plane = 1 })
        };

        var gameSettings = _gameSettings with
        {
            HexagonSettings = builder.ToImmutableDictionary(),
            StartMatter = 1000
        };
        var gameState = new GameState(gameSettings);

        var commands = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
            {
                "Player01", Command.Create([(new Hexagon(-1,0), new Command() { Training = new Army() { Circle = 1, Square = 1, Triangle = 1 } })])
            },
        };

        // Act
        var validations = gameState.Validate(commands.Keys.Single(), commands.Single().Value);
        (validations.ShouldHaveSingleItem()
             as InsufficientSpace)!.Required.ShouldBe(9 + 9);
    }
}
