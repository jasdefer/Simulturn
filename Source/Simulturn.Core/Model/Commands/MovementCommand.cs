namespace Simulturn.Core.Model.Commands;

public sealed record MovementCommand
{
    public Hexagon Destination { get; init; }
    public Army Army { get; init; }
}