
using Shouldly;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;
public class GameStateTest
{
    private static readonly GameSettings _gameSettings = new()
    {
        Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
        ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
        CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
        ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
        FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
        HexagonSettings = new Dictionary<Hexagon, HexagonSettings>
        {
            {
                new Hexagon(-1, 0), new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player01",new Army(){Dot = 5}, new Compound(){Plane = 1})
                }
            },
            {
                new Hexagon(1, 0), new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player02",new Army(){Dot = 5}, new Compound(){Plane = 1})
                }
            },
            {
                new Hexagon(0,0), HexagonSettings.Empty
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
        Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
        ProvidedSpace = new Compound() { Axis = 10, Dome = 0, Cube = 0, Plane = 10, Pyramid = 0 },
        RequiredSpace = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 1 },
        Seed = 1,
        StartMatter = 500,
        StructureDamage = new Army() { Triangle = 4, Circle = 4, Square = 4, Dot = 1 },
        TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
    };

    [Test]
    public void Initialize()
    {
        var gameState = new GameState(_gameSettings);
        gameState.Turn.ShouldBe((ushort)0);
        gameState.Hexagons.Count.ShouldBe(7);
        gameState.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        gameState.Constructions.ShouldBeEmpty();
        gameState.Trainings.ShouldBeEmpty();
        gameState.GameSettings.ShouldBe(_gameSettings);
        gameState.PlayerArmies["Player01"][new Hexagon(-1, 0)].ShouldBe(new Army() { Dot = 5 });
        gameState.PlayerArmies["Player01"].Count.ShouldBe(1);
        gameState.PlayerArmies["Player02"][new Hexagon(1, 0)].ShouldBe(new Army() { Dot = 5 });
        gameState.PlayerArmies["Player02"].Count.ShouldBe(1);
        gameState.PlayerCompounds["Player01"][new Hexagon(-1, 0)].ShouldBe(new Compound() { Plane = 1 });
        gameState.PlayerCompounds["Player01"].Count.ShouldBe(1);
        gameState.PlayerCompounds["Player02"][new Hexagon(1, 0)].ShouldBe(new Compound() { Plane = 1 });
        gameState.PlayerCompounds["Player02"].Count.ShouldBe(1);
        gameState.RemainingMatter.Sum(x => x.Value).ShouldBe(20000);
        gameState.RemainingMatter[new Hexagon(-1, 0)].ShouldBe(10000);
        gameState.RemainingMatter[new Hexagon(1, 0)].ShouldBe(10000);
        gameState.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        gameState.PlayerStates["Player01"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500
        });
        gameState.PlayerStates["Player02"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500
        });
        var dict = new Dictionary<string, Dictionary<Hexagon, Command>>();
        var newTurn = gameState.NextTurn(dict);
        newTurn.Turn.ShouldBe((ushort)1);
    }

    [Test]
    public void EmptyTurn()
    {
        var gameState = new GameState(_gameSettings);
        var dict = new Dictionary<string, Dictionary<Hexagon, Command>>();
        var newTurn = gameState.NextTurn(dict);
        newTurn.Turn.ShouldBe((ushort)1);
        newTurn.Hexagons.Count.ShouldBe(7);
        newTurn.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        newTurn.Constructions.ShouldBeEmpty();
        newTurn.Trainings.ShouldBeEmpty();
        newTurn.GameSettings.ShouldBe(_gameSettings);
        newTurn.PlayerArmies["Player01"][new Hexagon(-1, 0)].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerArmies["Player01"].Count.ShouldBe(1);
        newTurn.PlayerArmies["Player02"][new Hexagon(1, 0)].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerArmies["Player02"].Count.ShouldBe(1);
        newTurn.PlayerCompounds["Player01"][new Hexagon(-1, 0)].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerCompounds["Player01"].Count.ShouldBe(1);
        newTurn.PlayerCompounds["Player02"][new Hexagon(1, 0)].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerCompounds["Player02"].Count.ShouldBe(1);
        newTurn.RemainingMatter.Sum(x => x.Value).ShouldBe(20000-2*50);
        newTurn.RemainingMatter[new Hexagon(-1, 0)].ShouldBe(10000-50);
        newTurn.RemainingMatter[new Hexagon(1, 0)].ShouldBe(10000-50);
        newTurn.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        newTurn.PlayerStates["Player01"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500 + 50
        });
        newTurn.PlayerStates["Player02"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500 + 50
        });
    }

    [Test]
    public void TrainDots()
    {
        var gameState = new GameState(_gameSettings);
        var dict = new Dictionary<string, Dictionary<Hexagon, Command>>()
        {
        };
        var newTurn = gameState.NextTurn(dict);
        newTurn.Turn.ShouldBe((ushort)1);
        newTurn.Hexagons.Count.ShouldBe(7);
        newTurn.Hexagons.Select(hexagon => hexagon.X + hexagon.Y + hexagon.Z)
            .Distinct()
            .Single()
            .ShouldBe(0);
        newTurn.Constructions.ShouldBeEmpty();
        newTurn.Trainings.ShouldBeEmpty();
        newTurn.GameSettings.ShouldBe(_gameSettings);
        newTurn.PlayerArmies["Player01"][new Hexagon(-1, 0)].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerArmies["Player01"].Count.ShouldBe(1);
        newTurn.PlayerArmies["Player02"][new Hexagon(1, 0)].ShouldBe(new Army() { Dot = 5 });
        newTurn.PlayerArmies["Player02"].Count.ShouldBe(1);
        newTurn.PlayerCompounds["Player01"][new Hexagon(-1, 0)].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerCompounds["Player01"].Count.ShouldBe(1);
        newTurn.PlayerCompounds["Player02"][new Hexagon(1, 0)].ShouldBe(new Compound() { Plane = 1 });
        newTurn.PlayerCompounds["Player02"].Count.ShouldBe(1);
        newTurn.RemainingMatter.Sum(x => x.Value).ShouldBe(20000 - 2 * 50);
        newTurn.RemainingMatter[new Hexagon(-1, 0)].ShouldBe(10000 - 50);
        newTurn.RemainingMatter[new Hexagon(1, 0)].ShouldBe(10000 - 50);
        newTurn.PlayerStates.Keys.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        newTurn.PlayerStates["Player01"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500 + 50
        });
        newTurn.PlayerStates["Player02"].ShouldBe(new PlayerState()
        {
            AvailableSpace = 10,
            UsedSpace = 5,
            Matter = 500 + 50
        });
    }
}
