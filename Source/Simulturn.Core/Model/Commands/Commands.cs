namespace Simulturn.Core.Model.Commands;
public record Command
{
    public Compound Construction { get; init; } = Compound.Empty;
    public ImmutableArray<MovementCommand> MovementCommands { get; init; } = ImmutableArray<MovementCommand>.Empty;
    public Army Training { get; init; } = Army.Empty;
    public ImmutableHashSet<Upgrade> Upgrades { get; init; } = ImmutableHashSet<Upgrade>.Empty;

    public static Dictionary<Hexagon, Command> Create(params (Hexagon hexagon, Command command)[] commands)
    {
        return commands.ToDictionary(x => x.hexagon, x => x.command);
    }
}
