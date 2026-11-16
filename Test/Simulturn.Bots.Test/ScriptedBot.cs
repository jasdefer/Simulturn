using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Bots.Test;

/// <summary>
/// A bot for tests whose commands are a function of its view.
/// </summary>
public sealed class ScriptedBot(Func<PlayerGameState, IReadOnlyDictionary<Hexagon, Command>> script) : IBot
{
    public static IReadOnlyDictionary<Hexagon, Command> NoCommands { get; } = ImmutableDictionary<Hexagon, Command>.Empty;

    /// <summary>
    /// A bot that only acts on the first turn.
    /// </summary>
    public static ScriptedBot OnFirstTurn(Func<PlayerGameState, IReadOnlyDictionary<Hexagon, Command>> script)
    {
        return new ScriptedBot(view => view.Turn == 0 ? script(view) : NoCommands);
    }

    public static IReadOnlyDictionary<Hexagon, Command> Move(Hexagon from, Hexagon to, Army army)
    {
        return new Dictionary<Hexagon, Command>()
        {
            { from, new Command() { MovementCommands = [new MovementCommand() { Army = army, Destination = to }] } }
        };
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState view)
    {
        return script(view);
    }
}
