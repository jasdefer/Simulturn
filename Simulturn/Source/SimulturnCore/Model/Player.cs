namespace SimulturnCore.Model;
public class Player
{
    public Dictionary<Coordinates, Army> Armies { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Structure> Structures { get; set; } = new Dictionary<Coordinates, Structure>();
    public Dictionary<Coordinates, Army> Queue { get; set; } = new Dictionary<Coordinates, Army>();
    public Dictionary<Coordinates, Action> Actions { get; set; } = new Dictionary<Coordinates, Action>();
}
