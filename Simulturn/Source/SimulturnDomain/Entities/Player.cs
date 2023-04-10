using SimulturnDomain.Entities.Change;

namespace SimulturnDomain.Entities;
public class Player
{
    public required string Id { get; set; }
    public IReadOnlyDictionary<Coordinates, Army> Armies { get; set; } = new Dictionary<Coordinates, Army>();
    public IReadOnlyDictionary<Coordinates, Structure> Structures { get; set; } = new Dictionary<Coordinates, Structure>();
    public ushort Matter { get; set; } = 0;
    public IReadOnlyDictionary<ushort, ChangeSummary> ChangesPerTurn { get; set; } = new Dictionary<ushort, ChangeSummary>();
    public IReadOnlyDictionary<ushort, Order> OrdersPerTurn { get; set; } = new Dictionary<ushort, Order>();
    public bool EndedCurrentTurn { get; set; } = false;
}
