namespace SimulturnCore.Model.Order;
public class MoveOrder
{
    public int Player { get; set; }
    public Coordinates Origin { get; set; }
    public Coordinates Destination { get; set; }
    public Army Army { get; set; }
}
