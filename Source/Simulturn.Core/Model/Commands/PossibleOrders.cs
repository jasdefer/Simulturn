namespace Simulturn.Core.Model.Commands;

/// <summary>
/// What a player can still order in a turn, after the commands entered so far. Computed by
/// <see cref="Engine.Orders.Possible"/>.
/// </summary>
/// <param name="Budget">The matter and the space the commands entered so far leave.</param>
/// <param name="Hexagons">The hexagons on which something can still be ordered.</param>
public sealed record PossibleOrders(OrderCost Budget, ImmutableDictionary<Hexagon, PossibleHexagonOrders> Hexagons);

/// <summary>
/// What a player can still order on one hexagon. It has the shape of a <see cref="Command"/>, with
/// limits in place of amounts. Every limit stands for its order alone: any single order up to its
/// limit leaves the commands valid, two orders together may not, because they share the budget
/// and the dots. So a client takes one order, adds it to the commands and asks again.
/// </summary>
/// <param name="Training">How many more units of every type can be put into training.</param>
/// <param name="Construction">How many more buildings of every type can be started.</param>
/// <param name="Research">The upgrades whose research can be started here.</param>
/// <param name="Movable">
/// The units that can still leave the hexagon. Where they get to is told by
/// <see cref="Engine.Movement.Destinations"/>.
/// </param>
public sealed record PossibleHexagonOrders(Army Training, Compound Construction, ImmutableArray<Upgrade> Research, Army Movable)
{
    public bool IsEmpty => Training.IsEmpty && Construction.IsEmpty && Research.IsEmpty && Movable.IsEmpty;
}
