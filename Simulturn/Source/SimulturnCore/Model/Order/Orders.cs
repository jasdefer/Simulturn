namespace SimulturnCore.Model.Order;
public class Orders
{
    public List<ConstructionOrder> ConstructionOrders { get; set; } = new List<ConstructionOrder>();
    public List<TrainingOrder> TrainingOrders { get; set; } = new List<TrainingOrder>();
    public List<MoveOrder> MoveOrders { get; set; } = new List<MoveOrder>();
}
