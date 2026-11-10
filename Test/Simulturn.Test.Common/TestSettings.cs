using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Test.Common;

/// <summary>
/// The standard rules every test plays with. Tests only differ in their map (see <see cref="Maps"/>)
/// and, rarely, in the start matter. Keep this the single copy of the rules in the test projects.
/// </summary>
public static class TestSettings
{
    public const int DefaultStartMatter = 500;

    public static GameSettings Create(ImmutableDictionary<Hexagon, HexagonSettings> hexagonSettings, int startMatter = DefaultStartMatter)
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
            StartMatter = startMatter,
            PartialVisibilityRange = 2,
            VisibilityRange = 1,
            StructureDamage = new Army() { Triangle = 5, Circle = 5, Square = 5, Dot = 1 },
            TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
            StartUpgrades = ImmutableDictionary<string, ImmutableArray<Upgrade>>.Empty,
            Upgrades = new IUpgrade[]
                {
                    new DotUpgrade() { Cost = 150, Duration = 3, ExponentBonus = 20 },
                    new DotUpgrade() { Cost = 175, Duration = 4, ExponentBonus = 20 },
                    new UpgradeStartMatter() { Cost = 100, Duration = 2, Matter = 0 }
                }.ToDictionary(),
            HexagonSettings = hexagonSettings
        };
    }
}
