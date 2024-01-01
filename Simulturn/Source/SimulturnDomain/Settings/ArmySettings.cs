using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Settings;
public record ArmySettings(Army Cost,
    Army RequiredSpace,
    Army TrainingDuration,
    Army Income,
    Army StartUnits,
    Army StructureDamage)
{
    public static ArmySettings Default()
    {
        return new ArmySettings(Cost: new Army(100, 100, 100, 200, 60),
            RequiredSpace: new Army(3, 3, 3, 2, 1),
            TrainingDuration: new Army(2, 2, 2, 3, 1),
            Income: new Army(0, 0, 0, 0, 20),
            StartUnits: new Army(0, 0, 0, 0, 5),
            StructureDamage: new Army(100, 100, 100, 0, 5));
    }
};
