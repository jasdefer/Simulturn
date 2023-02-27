namespace SimulturnCore.Model;
public class Player
{
    public byte Id { get; set; }
    public Dictionary<Coordinates, Army> Armies { get; set; } = new();
    public Dictionary<Coordinates, Structure> Structures { get; set; } = new();
    public TurnCoordinates<Army> ArmyChanges { get; set; } = new();
    public TurnCoordinates<Structure> StructureChanges { get; set; } = new();
    public Dictionary<Coordinates, Army> TrainingQueue { get; set; } = new();
    public Dictionary<Coordinates, Structure> BuildingQueue { get; set; } = new();
    public Dictionary<ushort, Movement> Movements { get; set; } = new();
    public Dictionary<ushort, Army> TrainedArmiesPerTurn { get; set; } = new();
    public Dictionary<ushort, Structure> BuiltStructuresPerTurn { get; set; } = new();
    public ushort Matter { get; set; }
}
