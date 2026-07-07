using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.AI.Evaluation;

/// <summary>
/// Creates standard game settings for evaluations until a real map generator exists.
/// All maps are hexagon discs with two players starting on opposite edges; the presets
/// differ in how matter and researchable upgrades are distributed.
/// </summary>
public static class GameSettingsFactory
{
    /// <summary>
    /// Matter on every hexagon, upgrades researchable in the center.
    /// Expanding is always possible and never contested for long.
    /// </summary>
    public static GameSettings HexDisc(int radius = 3, int matterPerHexagon = 1500, int seed = 1)
    {
        Hexagon center = new(0, 0);
        return Build(radius,
            seed,
            matterSelector: _ => matterPerHexagon,
            researchSelector: hexagon => hexagon == center
                ? [Upgrade.DotUpgrade, Upgrade.DotUpgrade]
                : ImmutableArray<Upgrade>.Empty);
    }

    /// <summary>
    /// All matter sits on the two starting hexagons, expanding is impossible.
    /// Upgrades are researchable at home. A pure production and timing fight.
    /// </summary>
    public static GameSettings NoExpansion(int radius = 2, int startHexagonMatter = 12000, int seed = 1)
    {
        return Build(radius,
            seed,
            matterSelector: hexagon => IsStartingHexagon(radius, hexagon) ? startHexagonMatter : 0,
            researchSelector: hexagon => IsStartingHexagon(radius, hexagon)
                ? [Upgrade.DotUpgrade, Upgrade.DotUpgrade]
                : ImmutableArray<Upgrade>.Empty);
    }

    /// <summary>
    /// Small starting reserves, one safe home expansion per player and a rich contested
    /// center where the upgrades are researchable. Expanding and fighting for the middle
    /// is mandatory.
    /// </summary>
    public static GameSettings RichExpansions(int seed = 1)
    {
        const int radius = 3;
        Hexagon center = new(0, 0);
        var expansionMatter = new Dictionary<Hexagon, int>()
        {
            { new Hexagon(-2, 1), 4000 }, // home expansion of player 1
            { new Hexagon(2, -1), 4000 }, // home expansion of player 2
            { center, 6000 }
        };
        return Build(radius,
            seed,
            matterSelector: hexagon => IsStartingHexagon(radius, hexagon)
                ? 2000
                : expansionMatter.GetValueOrDefault(hexagon),
            researchSelector: hexagon => hexagon == center
                ? [Upgrade.DotUpgrade, Upgrade.DotUpgrade]
                : ImmutableArray<Upgrade>.Empty);
    }

    private static bool IsStartingHexagon(int radius, Hexagon hexagon)
    {
        return hexagon.Y == 0 && Math.Abs(hexagon.X) == radius;
    }

    private static GameSettings Build(int radius,
        int seed,
        Func<Hexagon, int> matterSelector,
        Func<Hexagon, ImmutableArray<Upgrade>> researchSelector)
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
                    Matter = matterSelector(hexagon),
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                    ResearchableUpgrades = researchSelector(hexagon),
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
