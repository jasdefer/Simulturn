namespace SimulturnCore.Model.Modification;
public class Movement
{
    public Coordinates Origin { get; set; }
    public Coordinates Destination { get; set; }
    public Army Army { get; set; }
}
