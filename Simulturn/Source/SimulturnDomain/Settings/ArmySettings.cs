using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record ArmySettings(Army Cost,
    Army RequiredSpace,
    Army TrainingDuration,
    Army Income);
