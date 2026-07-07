using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// Never issues any command. The weakest possible baseline opponent.
/// </summary>
public class IdlePlayer : IArtificialPlayer
{
    public string Name => "Idle";

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        return new Dictionary<Hexagon, Command>();
    }
}
