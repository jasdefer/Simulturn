using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Bots.Catalog;

/// <summary>
/// Never gives an order. The floor every other bot must beat.
/// </summary>
public sealed class IdleBot : IBot
{
    private static readonly ImmutableDictionary<Hexagon, Command> _noCommands = ImmutableDictionary<Hexagon, Command>.Empty;

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState view)
    {
        return _noCommands;
    }
}
