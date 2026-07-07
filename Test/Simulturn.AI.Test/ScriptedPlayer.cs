using Simulturn.AI;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Test;

/// <summary>
/// A test player that delegates the command generation to a function.
/// </summary>
public class ScriptedPlayer : IArtificialPlayer
{
    private readonly Func<PlayerGameState, IReadOnlyDictionary<Hexagon, Command>> _getCommands;

    public ScriptedPlayer(Func<PlayerGameState, IReadOnlyDictionary<Hexagon, Command>> getCommands)
    {
        _getCommands = getCommands;
    }

    public string Name => "Scripted";

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        return _getCommands(playerGameState);
    }
}
