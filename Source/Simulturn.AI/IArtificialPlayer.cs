using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI;

/// <summary>
/// A computer controlled player.
/// One instance plays a single game for a single player and may keep internal state between turns.
/// </summary>
public interface IArtificialPlayer
{
    string Name { get; }

    /// <summary>
    /// Returns the commands for the current turn.
    /// Implementations only receive the partially informed view of the player and
    /// must return commands that pass <see cref="GameState.Validate"/>.
    /// </summary>
    IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState);
}
