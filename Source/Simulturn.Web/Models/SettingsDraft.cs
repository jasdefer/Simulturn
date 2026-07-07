using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.Web.Models;

/// <summary>
/// Mutable mirror of <see cref="GameSettings"/> for the settings editor.
/// <see cref="Validate"/> reports problems; <see cref="Build"/> produces the immutable settings
/// (map included, via <see cref="Services.MapGeneratorService"/>).
/// </summary>
public sealed class SettingsDraft
{
    public List<string> PlayerNames { get; } = ["Player 1", "Player 2"];

    public GameMode Mode { get; set; } = GameMode.God;

    public int StartMatter { get; set; }

    public int VisibilityRange { get; set; }

    public int PartialVisibilityRange { get; set; }

    public Dictionary<Unit, UnitSettingsDraft> Units { get; } = [];

    public Dictionary<Building, BuildingSettingsDraft> Buildings { get; } = [];

    public List<DotUpgradeDraft> DotUpgrades { get; } = [];

    public List<StartMatterUpgradeDraft> StartMatterUpgrades { get; } = [];

    public MapGeneratorOptions Map { get; } = new();

    public static SettingsDraft FromDefaults()
    {
        var defaults = Services.DefaultSettings.Standard(["Player 1", "Player 2"], seed: 1);
        var draft = new SettingsDraft
        {
            StartMatter = defaults.StartMatter,
            VisibilityRange = defaults.VisibilityRange,
            PartialVisibilityRange = defaults.PartialVisibilityRange,
        };
        foreach (var unit in Enum.GetValues<Unit>())
        {
            draft.Units[unit] = new UnitSettingsDraft
            {
                Cost = defaults.ArmyCost[unit],
                RequiredSpace = defaults.RequiredSpace[unit],
                TrainingDuration = defaults.TrainingDuration[unit],
                Income = defaults.Income[unit],
                StructureDamage = defaults.StructureDamage[unit],
                FightExponent = defaults.FightExponent[unit],
                MovementRange = defaults.MovementRange[unit],
            };
        }
        foreach (var building in Enum.GetValues<Building>())
        {
            draft.Buildings[building] = new BuildingSettingsDraft
            {
                Cost = defaults.CompoundCost[building],
                ProvidedSpace = defaults.ProvidedSpace[building],
                ConstructionDuration = defaults.ConstructionDuration[building],
                Armor = defaults.Armor[building],
            };
        }
        foreach (var upgrade in defaults.Upgrades.GetValueOrDefault(Upgrade.DotUpgrade, []).Cast<DotUpgrade>())
        {
            draft.DotUpgrades.Add(new DotUpgradeDraft
            {
                Cost = upgrade.Cost,
                Duration = upgrade.Duration,
                ExponentBonus = upgrade.ExponentBonus,
            });
        }
        draft.Map.Seed = Random.Shared.Next(1, 1_000_000);
        return draft;
    }

    public List<string> Validate()
    {
        List<string> problems = [];
        if (PlayerNames.Count is < 1 or > 4)
        {
            problems.Add("Between 1 and 4 players are supported (4 player colors).");
        }
        if (PlayerNames.Any(string.IsNullOrWhiteSpace))
        {
            problems.Add("Player names must not be empty.");
        }
        if (PlayerNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != PlayerNames.Count)
        {
            problems.Add("Player names must be unique.");
        }
        foreach (var (unit, values) in Units)
        {
            if (values.TrainingDuration < 1)
            {
                problems.Add($"{unit}: training duration must be at least 1 (0 would burn matter and never complete).");
            }
        }
        foreach (var (building, values) in Buildings)
        {
            if (values.ConstructionDuration < 1)
            {
                problems.Add($"{building}: construction duration must be at least 1.");
            }
            if (values.Armor < 1)
            {
                problems.Add($"{building}: armor must be at least 1 (structure damage is divided by armor).");
            }
        }
        foreach (var upgrade in DotUpgrades.Concat<UpgradeDraftBase>(StartMatterUpgrades))
        {
            if (upgrade.Duration < 1)
            {
                problems.Add("Upgrade durations must be at least 1.");
            }
        }
        if (Map.Radius < 1)
        {
            problems.Add("Map radius must be at least 1.");
        }
        if (Map.StartRingRadius < 1 || Map.StartRingRadius > Map.Radius)
        {
            problems.Add("Start ring radius must be between 1 and the map radius.");
        }
        if (Map.DotUpgradeLevelsAtCenter > DotUpgrades.Count)
        {
            problems.Add($"The center hex offers {Map.DotUpgradeLevelsAtCenter} DotUpgrade levels but only {DotUpgrades.Count} are defined.");
        }
        if (6 * Map.StartRingRadius < PlayerNames.Count)
        {
            problems.Add("The start ring is too small for the number of players.");
        }
        return problems;
    }

    public GameSettings Build()
    {
        List<IUpgrade> dotUpgrades = DotUpgrades
            .Select(IUpgrade (upgrade) => new DotUpgrade
            {
                Cost = ClampShort(upgrade.Cost),
                Duration = ClampByte(upgrade.Duration),
                ExponentBonus = ClampShort(upgrade.ExponentBonus),
            })
            .ToList();
        List<IUpgrade> startMatterUpgrades = StartMatterUpgrades
            .Select(IUpgrade (upgrade) => new UpgradeStartMatter
            {
                Cost = ClampShort(upgrade.Cost),
                Duration = ClampByte(upgrade.Duration),
                Matter = upgrade.Matter,
            })
            .ToList();

        return new GameSettings
        {
            StartMatter = StartMatter,
            Seed = Map.Seed,
            VisibilityRange = ClampByte(VisibilityRange),
            PartialVisibilityRange = ClampByte(PartialVisibilityRange),
            ArmyCost = ToArmy(unit => Units[unit].Cost),
            RequiredSpace = ToArmy(unit => Units[unit].RequiredSpace),
            TrainingDuration = ToArmy(unit => Units[unit].TrainingDuration),
            Income = ToArmy(unit => Units[unit].Income),
            MovementRange = ToArmy(unit => Units[unit].MovementRange),
            StructureDamage = ToArmy(unit => Units[unit].StructureDamage),
            FightExponent = ToArmy(unit => Units[unit].FightExponent),
            CompoundCost = ToCompound(building => Buildings[building].Cost),
            ProvidedSpace = ToCompound(building => Buildings[building].ProvidedSpace),
            ConstructionDuration = ToCompound(building => Buildings[building].ConstructionDuration),
            Armor = ToCompound(building => Buildings[building].Armor),
            StartUpgrades = ImmutableDictionary<string, ImmutableArray<Upgrade>>.Empty,
            Upgrades = dotUpgrades.Concat(startMatterUpgrades).ToDictionary(),
            HexagonSettings = Services.MapGeneratorService.Generate(Map, PlayerNames),
        };
    }

    private static Army ToArmy(Func<Unit, int> value) => new()
    {
        Triangle = ClampShort(value(Unit.Triangle)),
        Circle = ClampShort(value(Unit.Circle)),
        Square = ClampShort(value(Unit.Square)),
        Dot = ClampShort(value(Unit.Dot)),
    };

    private static Compound ToCompound(Func<Building, int> value) => new()
    {
        Plane = ClampShort(value(Building.Plane)),
        Axis = ClampShort(value(Building.Axis)),
        Dome = ClampShort(value(Building.Dome)),
        Pyramid = ClampShort(value(Building.Pyramid)),
        Cube = ClampShort(value(Building.Cube)),
    };

    private static short ClampShort(int value) => (short)Math.Clamp(value, 0, short.MaxValue);

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, byte.MaxValue);
}

public sealed class UnitSettingsDraft
{
    public int Cost { get; set; }
    public int RequiredSpace { get; set; }
    public int TrainingDuration { get; set; }
    public int Income { get; set; }
    public int StructureDamage { get; set; }
    public int FightExponent { get; set; }
    public int MovementRange { get; set; }
}

public sealed class BuildingSettingsDraft
{
    public int Cost { get; set; }
    public int ProvidedSpace { get; set; }
    public int ConstructionDuration { get; set; }
    public int Armor { get; set; }
}

public abstract class UpgradeDraftBase
{
    public int Cost { get; set; }
    public int Duration { get; set; } = 1;
}

public sealed class DotUpgradeDraft : UpgradeDraftBase
{
    public int ExponentBonus { get; set; }
}

public sealed class StartMatterUpgradeDraft : UpgradeDraftBase
{
    public int Matter { get; set; }
}
