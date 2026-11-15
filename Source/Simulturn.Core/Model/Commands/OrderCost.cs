namespace Simulturn.Core.Model.Commands;

/// <summary>
/// What orders cost a player: the matter that is paid and the space the trained units take.
/// Computed by <see cref="Engine.Costs"/>.
/// </summary>
public readonly record struct OrderCost(int Matter, int Space)
{
    public static OrderCost operator +(OrderCost left, OrderCost right)
    {
        return new OrderCost(left.Matter + right.Matter, left.Space + right.Space);
    }
}
