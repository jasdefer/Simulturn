namespace Simulturn.Core.Model.Commands;

public sealed record MovementOrder
{
    public Hexagon Destination { get; init; }
    public Army Army { get; init; }
}