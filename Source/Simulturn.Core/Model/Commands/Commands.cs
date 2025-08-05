namespace Simulturn.Core.Model.Commands;
public record Command
{
    public Compound? Construction { get; init; }
    public ImmutableArray<MovementCommand> MovementCommands { get; init; }
    public Army? Training { get; init; }
    public UpgradeCommand? UpgradeCommand { get; init; }
}
