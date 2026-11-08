using Shouldly;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Test.Common;

public static class GameStateExtensions
{
    /// <summary>
    /// Resolves one turn the way a server would: the commands must pass <see cref="GameState.Validate"/>,
    /// and the resulting state must satisfy every state invariant.
    /// </summary>
    public static GameState PlayTurn<TCommands>(this GameState state, IReadOnlyDictionary<string, TCommands> commands, bool validateCommands = true)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        if (validateCommands)
        {
            foreach ((string playerId, TCommands playerCommands) in commands)
            {
                state.Validate(playerId, playerCommands).ShouldBeEmpty($"The commands of {playerId} on turn {state.Turn} are invalid.");
            }
        }
        GameState next = state.NextTurn(commands);
        StateInvariants.CheckState(next).ShouldBeEmpty($"The state after turn {state.Turn} violates an invariant.");
        return next;
    }

    /// <summary>
    /// Resolves a turn without any commands.
    /// </summary>
    public static GameState PlayEmptyTurn(this GameState state)
    {
        return state.PlayTurn(new Dictionary<string, Dictionary<Hexagon, Command>>());
    }
}
