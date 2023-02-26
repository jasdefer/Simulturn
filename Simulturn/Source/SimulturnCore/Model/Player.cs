using SimulturnCore.Model.Modification;
using SimulturnCore.Model.Order;

namespace SimulturnCore.Model;
public class Player
{
    public byte Id { get; set; }
    public Dictionary<Coordinates, Army> Armies { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Structure> Structures { get; set; } = new Dictionary<Coordinates, Structure>();
    public Dictionary<ushort, Orders> OrdersPerTurn { get; set; } = new Dictionary<ushort, Orders>();
    public Dictionary<ushort, Modifications> Modifications { get; set; } = new Dictionary<ushort, Modifications>();
    public ushort Matter { get; set; }
}
