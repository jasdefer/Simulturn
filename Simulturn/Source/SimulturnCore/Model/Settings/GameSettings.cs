namespace SimulturnCore.Model.Settings;
public class GameSettings
{
    public ArmySettings Army { get; set; } = new ArmySettings();
    public StructureSettings Structure { get; set; } = new StructureSettings();
    public Dictionary<Coordinates, Army> MaxIncomeUnits { get; set; }
}
