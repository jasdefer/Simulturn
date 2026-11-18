namespace Simulturn.Core.Model.Commands;

/// <summary>
/// The commands of one player for one turn while they are entered. A client, a bot or a UI,
/// adds order by order and hands in <see cref="ToImmutable"/>: commands that were given never
/// change again. The builder checks no rule; what can still be ordered tells
/// <see cref="Engine.Orders.Possible"/>, asked with <see cref="Commands"/>.
/// </summary>
public sealed class CommandsBuilder
{
    private static readonly Command _noCommand = new();

    private readonly Dictionary<Hexagon, Command> _commands;

    public CommandsBuilder()
    {
        _commands = [];
    }

    /// <summary>
    /// Starts from the given commands, which stay untouched: the way to try an alternative
    /// on a copy or to continue commands that were handed in.
    /// </summary>
    public CommandsBuilder(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        _commands = new Dictionary<Hexagon, Command>(commands);
    }

    /// <summary>
    /// The commands entered so far, to ask the engine with. It is a view, not a copy: it
    /// shows every order added later.
    /// </summary>
    public IReadOnlyDictionary<Hexagon, Command> Commands => _commands;

    /// <summary>
    /// The command of the hexagon, empty if nothing is ordered there.
    /// </summary>
    public Command On(Hexagon hexagon)
    {
        return _commands.GetValueOrDefault(hexagon) ?? _noCommand;
    }

    /// <summary>
    /// Puts the units into training on the hexagon, on top of the ones already ordered there.
    /// </summary>
    public CommandsBuilder Train(Hexagon hexagon, Army training)
    {
        Command command = On(hexagon);
        _commands[hexagon] = command with { Training = command.Training + training };
        return this;
    }

    /// <summary>
    /// Starts the constructions on the hexagon, on top of the ones already ordered there.
    /// </summary>
    public CommandsBuilder Construct(Hexagon hexagon, Compound construction)
    {
        Command command = On(hexagon);
        _commands[hexagon] = command with { Construction = command.Construction + construction };
        return this;
    }

    /// <summary>
    /// Starts the research of the upgrade on the hexagon. A hexagon researches one upgrade at
    /// a time, so a research already requested there is replaced.
    /// </summary>
    public CommandsBuilder Research(Hexagon hexagon, Upgrade upgrade)
    {
        _commands[hexagon] = On(hexagon) with { Research = upgrade };
        return this;
    }

    /// <summary>
    /// Moves the army from the origin to the destination. Units already moving there are
    /// joined in one <see cref="MovementOrder"/>.
    /// </summary>
    public CommandsBuilder Move(Hexagon origin, Hexagon destination, Army army)
    {
        Command command = On(origin);
        MovementOrder? joined = command.Movements.FirstOrDefault(x => x.Destination == destination);
        _commands[origin] = command with
        {
            Movements = joined is null
                ? command.Movements.Add(new MovementOrder() { Destination = destination, Army = army })
                : command.Movements.Replace(joined, joined with { Army = joined.Army + army })
        };
        return this;
    }

    /// <summary>
    /// The commands as they are handed in.
    /// </summary>
    public ImmutableDictionary<Hexagon, Command> ToImmutable()
    {
        return _commands.ToImmutableDictionary();
    }
}
