using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record StructureSettings(Structure Cost,
    Structure ProvidedSpace,
    Structure ConstructionDuration)
{
    public static StructureSettings Default()
    {
        return new StructureSettings(
            Cost: new Structure(300, 150, 150, 150, 250, 75),
            ProvidedSpace: new Structure(10, 0, 0, 0, 0, 10),
            ConstructionDuration: new Structure(6, 4, 4, 4, 5, 3));
    }
};