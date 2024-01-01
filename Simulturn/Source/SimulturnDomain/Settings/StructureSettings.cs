using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Settings;
public record StructureSettings(Structure Cost,
    Structure ProvidedSpace,
    Structure ConstructionDuration,
    Structure StartStructures,
    Structure Armor)
{
    public static StructureSettings Default()
    {
        return new StructureSettings(
            Cost: new Structure(300, 150, 150, 150, 250, 75),
            ProvidedSpace: new Structure(10, 0, 0, 0, 0, 10),
            ConstructionDuration: new Structure(6, 4, 4, 4, 5, 3),
            StartStructures: new Structure(1, 0, 0, 0, 0, 0),
            Armor: new Structure(500, 100, 100, 100, 50, 50));
    }
};