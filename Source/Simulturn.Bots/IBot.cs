using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Bots;

/// <summary>
/// A player that is played by code. A bot is a client like any other: it only gets the view of
/// its player, never the full state, and every rule it needs it asks from the engine.
/// One instance plays one game on one seat and may keep what it has seen.
/// </summary>
public interface IBot
{
    /// <summary>
    /// The commands of the bot for the turn of the view. Called once per turn, in turn order,
    /// as long as its player is not defeated. The commands must be valid.
    /// </summary>
    IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState view);
}
