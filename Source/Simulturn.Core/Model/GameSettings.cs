using System.Collections.Frozen;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Model;

public sealed record GameSettings
{
    public GameSettings()
    {
    }

    public int StartMatter { get; init; }
    public required Army ArmyCost { get; init; }
    public required Army RequiredSpace { get; init; }
    public required Army TrainingDuration { get; init; }
    public required Army Income { get; init; }
    public required Army StructureDamage { get; init; }
    public required byte PartialVisibilityRange { get; init; }
    public required byte VisibilityRange { get; init; }

    /// <summary>
    /// The values are divided by 100, so 100 means 1.0. It is the exponent for the fight calculation.
    /// </summary>
    public required Army FightExponent { get; init; }
    public required Compound CompoundCost { get; init; }
    public required Compound ProvidedSpace { get; init; }
    public required Compound ConstructionDuration { get; init; }
    public required Compound Armor { get; init; }
    public required FrozenDictionary<Hexagon, HexagonSettings> HexagonSettings { get; init; }
    public required FrozenDictionary<string, ImmutableArray<Upgrade>> StartUpgrades { get; init; }
    public required FrozenDictionary<Upgrade, ImmutableArray<IUpgrade>> Upgrades { get; init; }
    public int Seed { get; init; }
    public static Building TrainingBuilding(Unit unit) => unit switch
    {
        Unit.Dot => Building.Plane,
        Unit.Circle => Building.Dome,
        Unit.Triangle => Building.Pyramid,
        Unit.Square => Building.Cube,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };
}