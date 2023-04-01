using SimulturnDomain.Entities.Change;

namespace SimulturnDomain.Entities;
public class Player
{
    public byte Id { get; set; }
    public Dictionary<Coordinates, Army> Armies { get; set; } = new();
    public Dictionary<Coordinates, Structure> Structures { get; set; } = new();
    public ushort Matter { get; set; }
    public Dictionary<ushort, ChangeSummary> ChangesPerTurn { get; set; } = new Dictionary<ushort, ChangeSummary>();
    public Dictionary<ushort, Order> OrdersPerTurn { get; set; } = new Dictionary<ushort, Order>();
}
