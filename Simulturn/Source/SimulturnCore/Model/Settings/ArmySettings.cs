namespace SimulturnCore.Model.Settings;
public class ArmySettings
{
    public Army Cost { get; set; } = new Army(100, 100, 100, 200, 50);
    public Army RequiredSpace { get; set; } = new Army(3, 3, 3, 2, 1);
    public Army TrainingDuration { get; set; } = new Army(3, 3, 3, 5, 2);
}
