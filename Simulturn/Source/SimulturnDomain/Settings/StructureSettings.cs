using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record StructureSettings(Structure Cost,
    Structure ProvidedSpace,
    Structure ConstructionDuration);