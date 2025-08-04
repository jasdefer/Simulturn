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
    public required Army FightExponent { get; init; }
    public required Army MaximumNumberOfResourceGatheringUnits { get; init; }
    public required Compound CompoundCost { get; init; }
    public required Compound ProvidedSpace { get; init; }
    public required Compound ConstructionDuration { get; init; }
    public required Compound Armor { get; init; }
    public required ImmutableDictionary<Hexagon, HexagonSettings> HexagonSettings { get; init; }
    public int Seed { get; init; }
}