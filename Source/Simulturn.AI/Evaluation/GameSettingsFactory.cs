using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.AI.Evaluation;

/// <summary>
/// Creates standard game settings for evaluations until a real map generator exists.
/// </summary>
public static class GameSettingsFactory
{
    /// <summary>
    /// A symmetric hexagon disc with two players starting on opposite edges.
    /// </summary>
    public static GameSettings HexDisc(int radius = 3, int matterPerHexagon = 1500, int seed = 1)
    {
        Dictionary<Hexagon, HexagonSettings> hexagonSettings = [];
        for (short x = (short)-radius; x <= radius; x++)
        {
            for (short y = (short)-radius; y <= radius; y++)
            {
                if (Math.Abs(x + y) > radius)
                {
                    continue;
                }
                Hexagon hexagon = new(x, y);
                hexagonSettings[hexagon] = new HexagonSettings()
                {
                    Matter = matterPerHexagon,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                    ResearchableUpgrades = hexagon == new Hexagon(0, 0) ? [Upgrade.DotUpgrade, Upgrade.DotUpgrade] : [],
                    PlayerInitialization = hexagon switch
                    {
                        { Y: 0 } when hexagon.X == -radius => ("Player01", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        { Y: 0 } when hexagon.X == radius => ("Player02", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        _ => ((string, Army, Compound)?)null
                    }
                };
            }
        }
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
            Seed = seed,
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
            HexagonSettings = hexagonSettings.ToImmutableDictionary()
        };
    }
}
