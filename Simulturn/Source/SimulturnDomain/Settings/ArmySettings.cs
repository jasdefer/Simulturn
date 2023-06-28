using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record ArmySettings(Army Cost,
    Army RequiredSpace,
    Army TrainingDuration,
    Army Income)
{
    public static ArmySettings Default()
    {
        return new ArmySettings(Cost: new Army(100, 100, 100, 200, 60),
            RequiredSpace: new Army(3, 3, 3, 2, 1),
            TrainingDuration: new Army(2, 2, 2, 3, 1),
            Income: new Army(0, 0, 0, 0, 20));
    }
};
