
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
        Armor = new Compound() { Axis = 30, Dome = 10, Cube = 20, Plane = 20, Pyramid = 20 },
        ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
        CompoundCost = new Compound() { Axis = 350, Dome = 100, Cube = 150, Plane = 150, Pyramid = 150 },
        ConstructionDuration = new Compound() { Axis = 4, Dome = 2, Cube = 3, Plane = 3, Pyramid = 3 },
        FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
        HexagonSettings = new Dictionary<Hexagon, HexagonSettings>
        {
            {
                new Hexagon(-1, 0), new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player01",new Army(){Dot = 5}, new Compound(){Axis = 1})
                }
            },
            {
                new Hexagon(1, 0), new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 10000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 12 },
                    PlayerInitialization = new ("Player02",new Army(){Dot = 5}, new Compound(){Axis = 1})
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
        ProvidedSpace = new Compound() { Axis = 10, Dome = 10, Cube = 0, Plane = 0, Pyramid = 0 },
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
        gameState.PlayerCompounds["Player01"][new Hexagon(-1, 0)].ShouldBe(new Compound() { Axis = 1 });
        gameState.PlayerCompounds["Player01"].Count.ShouldBe(1);
        gameState.PlayerCompounds["Player02"][new Hexagon(1, 0)].ShouldBe(new Compound() { Axis = 1 });
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
}
