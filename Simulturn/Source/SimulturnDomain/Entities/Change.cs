namespace SimulturnDomain.Entities;
public class Change
{
    public Change()
    {
        Income = new Income(0, 0);
        Movements = new List<Movement>();
        TrainedArmiesPerCoordinates = new Dictionary<Coordinates, Army>();
        BuiltStructuresPerCoordinates = new Dictionary<Coordinates, Structure>();
    }
    public Income Income { get; set; }
    public List<Movement> Movements { get; }
    public Dictionary<Coordinates, Army> TrainedArmiesPerCoordinates { get; }
    public Dictionary<Coordinates, Structure> BuiltStructuresPerCoordinates { get; }
}