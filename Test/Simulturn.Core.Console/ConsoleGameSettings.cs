using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.Core.Console;

internal class ConsoleGameSettings
{
    public static readonly GameSettings DefaultGame = new()
    {
        Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
        ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
        CompoundCost = new Compound() { Axis = 100, Dome = 150, Cube = 150, Plane = 350, Pyramid = 150 },
        ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
        FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
        Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
        MovementRange = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
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
}
