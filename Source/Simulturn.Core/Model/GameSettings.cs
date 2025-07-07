using System.Collections.Immutable;
using Simulturn.Core.Model;

public record GameSettings(
    double BaseFightExponent,
    int StartMatter,
    Army ArmyCost,
    Army RequiredSpace,
    Army TrainingDuration,
    Army Income,
    Army InitialArmy,
    Army StructureDamage,
    Army FightExponent,
    Compound CompoundCost,
    Compound ProvidedSpace,
    Compound ConstructionDuration,
    Compound InitialCompound,
    Compound Armor,
    ImmutableDictionary<Hexagon, HexagonSettings> HexagonSettings,
    int Seed = 1
);