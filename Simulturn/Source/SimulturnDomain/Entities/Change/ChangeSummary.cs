namespace SimulturnDomain.Entities.Change;
public class ChangeSummary
{
    public IEnumerable<Movement> Movements { get; set; } = new List<Movement>();
    public Dictionary<Coordinates, Army> ArmyLosses { get; set; } = new Dictionary<Coordinates, Army>();
    public Income Income { get; set; } = new Income();
    public Dictionary<Coordinates, Army> TrainedArmies { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Structure> BuiltStructures { get; set; } = new Dictionary<Coordinates, Structure>();
}
