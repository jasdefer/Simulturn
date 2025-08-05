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
    /// The values are divided by 100, so 100 means 1.0. It is the exponent for the fight calculation.
    /// </summary>
    public required Army FightExponent { get; init; }
    public required Compound CompoundCost { get; init; }
    public required Compound ProvidedSpace { get; init; }
    public required Compound ConstructionDuration { get; init; }
    public required Compound Armor { get; init; }
    public required ImmutableDictionary<Hexagon, HexagonSettings> HexagonSettings { get; init; }
    public int Seed { get; init; }
}