namespace SimulturnCore.Model;
public class Action
{
    public ushort TurnNumber { get; set; }
    public Army Training { get; set; }
    public Army Arrivals { get; set; }
    public Army Departures { get; set; }
    public Structure Constructions { get; set; }
}
