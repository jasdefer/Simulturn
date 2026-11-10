namespace Simulturn.Core.Model.Commands;

public sealed record Command
{
    public Compound Construction { get; init; } = Compound.Empty;
    public ImmutableArray<MovementCommand> MovementCommands { get; init; } = [];
    public Army Training { get; init; } = Army.Empty;
    public Upgrade? Research { get; init; }

    public static Dictionary<Hexagon, Command> Create(params (Hexagon hexagon, Command command)[] commands)
    {
        return commands.ToDictionary(x => x.hexagon, x => x.command);
    }
}
