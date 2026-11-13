using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Frozen;

namespace Simulturn.Core.Setup;

/// <summary>
/// The standard rules of the game: costs, durations, armor, income and upgrades. This is the only
/// copy; tests, console, arena and a UI all start from here and differ only in their map.
/// </summary>
public static class DefaultSettings
{
    public const int DefaultStartMatter = 500;

    /// <summary>
    /// The standard rules on the given map, see <see cref="Maps"/> and <see cref="MapGenerator"/>.
    /// </summary>
    public static GameSettings Create(IReadOnlyDictionary<Hexagon, HexagonSettings> hexagonSettings, int startMatter = DefaultStartMatter, int seed = 1)
    {
        return new GameSettings()
        {
            Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
            ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
            CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
            ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
            FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
            Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
            MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 1, Dot = 1 },
            ProvidedSpace = new Compound() { Axis = 10, Dome = 0, Cube = 0, Plane = 10, Pyramid = 0 },
            RequiredSpace = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 1 },
            Seed = seed,
            StartMatter = startMatter,
            PartialVisibilityRange = 2,
            VisibilityRange = 1,
            StructureDamage = new Army() { Triangle = 5, Circle = 5, Square = 5, Dot = 1 },
            TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
            StartUpgrades = FrozenDictionary<string, ImmutableArray<Upgrade>>.Empty,
            Upgrades = new IUpgradeLevel[]
                {
                    new DotUpgrade() { Cost = 150, Duration = 3, ExponentBonus = 20 },
                    new DotUpgrade() { Cost = 175, Duration = 4, ExponentBonus = 20 },
                    new UpgradeStartMatter() { Cost = 100, Duration = 2, Matter = 0 }
                }.ToDictionary(),
            HexagonSettings = hexagonSettings.ToFrozenDictionary()
        };
    }

    /// <summary>
    /// The standard rules on a generated map. The seed of the options is also the seed of the game.
    /// </summary>
    public static GameSettings Create(MapGeneratorOptions options, int startMatter = DefaultStartMatter)
    {
        return Create(MapGenerator.Generate(options), startMatter, options.Seed);
    }
}
