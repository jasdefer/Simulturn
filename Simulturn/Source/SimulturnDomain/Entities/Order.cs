namespace SimulturnDomain.Entities;
public class Order
{
    public Dictionary<Coordinates, Army> Training { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Structure> Construction { get; set; } = new Dictionary<Coordinates, Structure>();
    public IEnumerable<Movement> Movements { get; set; } = new List<Movement>();
}
