namespace SimulturnCore.Model.Settings;
public class Settings
{
    public ArmySettings Army { get; set; } = new ArmySettings();
    public StructureSettings Structure { get; set; } = new StructureSettings();
}
