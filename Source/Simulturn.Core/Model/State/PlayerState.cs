namespace Simulturn.Core.Model.State;

public record PlayerState
{
    public int Matter { get; init; }
    public int UsedSpace { get; init; }
    public int AvailableSpace { get; init; }
}