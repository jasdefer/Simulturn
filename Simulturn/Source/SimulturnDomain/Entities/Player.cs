namespace SimulturnDomain.Entities;
public class Player
{
    public required string Name { get; set; }
    public Dictionary<Coordinates, Army> Armies { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Structure> Structures { get; set; } = new Dictionary<Coordinates, Structure>();
    public ushort Matter { get; set; } = 0;
    public Dictionary<ushort, Change> ChangesPerTurn { get; set; } = new Dictionary<ushort, Change>();
    public Dictionary<ushort, Order> OrdersPerTurn { get; set; } = new Dictionary<ushort, Order>();
    public bool EndedCurrentTurn { get; set; } = false;
}
