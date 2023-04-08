using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public class GameSettings
{
    public ArmySettings Army { get; set; } = new ArmySettings();
    public StructureSettings Structure { get; set; } = new StructureSettings();
    public Dictionary<Coordinates, Army> MaxNumberOfUnitsGeneratingIncome { get; set; } = new Dictionary<Coordinates, Army>();
    public IEnumerable<Coordinates> Coordinates { get; set; } = new List<Coordinates>();
}
