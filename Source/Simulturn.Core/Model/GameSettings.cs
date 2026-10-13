using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Model;

public record GameSettings
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

    /// <summary>
    /// The maximum number of hexagons each unit type can move in a single turn.
    /// </summary>
    public required Army MovementRange { get; init; }
    public required byte PartialVisibilityRange { get; set; }
    public required byte VisibilityRange { get; set; }

    /// <summary>
    /// The values are divided by 100, so 100 means 1.0. It is the exponent for the fight calculation.
    /// </summary>
    public required Army FightExponent { get; init; }
    public required Compound CompoundCost { get; init; }
    public required Compound ProvidedSpace { get; init; }
    public required Compound ConstructionDuration { get; init; }
    public required Compound Armor { get; init; }
    public required ImmutableDictionary<Hexagon, HexagonSettings> HexagonSettings { get; init; }
    public required ImmutableDictionary<string, ImmutableArray<Upgrade>> StartUpgrades { get; init; }
    public required ImmutableDictionary<Upgrade, ImmutableArray<IUpgrade>> Upgrades { get; init; }
    public int Seed { get; init; }
    public static ImmutableDictionary<Unit, Building> TrainingBuildingPerUnit => new Dictionary<Unit, Building>
        {
            { Unit.Dot, Building.Plane },
            { Unit.Circle, Building.Dome },
            { Unit.Triangle, Building.Pyramid },
            { Unit.Square, Building.Cube }
        }.ToImmutableDictionary();
}