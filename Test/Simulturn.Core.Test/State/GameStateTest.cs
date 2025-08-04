
using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;
public class GameStateTest
{
    private static readonly GameSettings _gameSettings = new()
    {
        Armor = new Compound() { Axis = 1, Dome = 1, Cube = 1, Plane = 1, Pyramid = 1 },
        ArmyCost = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        CompoundCost = new Compound() { Axis = 1, Dome = 1, Cube = 1, Plane = 1, Pyramid = 1 },
        ConstructionDuration = new Compound() { Axis = 1, Dome = 1, Cube = 1, Plane = 1, Pyramid = 1 },
        FightExponent = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        HexagonSettings = ImmutableDictionary<Hexagon, HexagonSettings>.Empty,
        Income = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        MaximumNumberOfResourceGatheringUnits = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        ProvidedSpace = new Compound() { Axis = 1, Dome = 1, Cube = 1, Plane = 1, Pyramid = 1 },
        RequiredSpace = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        Seed = 1,
        StartMatter = 1000,
        StructureDamage = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
        TrainingDuration = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
    };

    [Test]
    public void Test()
    {

    }
}
