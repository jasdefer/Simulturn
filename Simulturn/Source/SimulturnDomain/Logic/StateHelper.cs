using SimulturnDomain.Model;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class StateHelper
{
    /// <summary>
    /// Compute the current state of the game based on all orders by each player.
    /// </summary>
    public static ImmutableDictionary<string, State> GetStatesPerPlayer(Game game)
    {
        throw new NotImplementedException();
    }
}