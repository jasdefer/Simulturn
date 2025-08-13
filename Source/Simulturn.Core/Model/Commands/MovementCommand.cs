namespace Simulturn.Core.Model.Commands;

public record MovementCommand
{
    public Hexagon Destination { get; init; }
    public Army Army { get; init; }
}