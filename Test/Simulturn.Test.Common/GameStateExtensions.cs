using Shouldly;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Test.Common;

public static class GameStateExtensions
{
    extension(GameState state)
    {
        /// <summary>
        /// Resolves one turn the way a server would: the commands must pass <see cref="CommandValidator.Validate"/>,
        /// and the resulting state must satisfy every state invariant.
        /// </summary>
        public GameState PlayTurn<TCommands>(IReadOnlyDictionary<string, TCommands> commands, bool validateCommands = true)
            where TCommands : IReadOnlyDictionary<Hexagon, Command>
        {
            if (validateCommands)
            {
                foreach ((string playerId, TCommands playerCommands) in commands)
                {
                    CommandValidator.Validate(state, playerId, playerCommands).ShouldBeEmpty($"The commands of {playerId} on turn {state.Turn} are invalid.");
                }
            }
            GameState next = TurnResolution.Resolve(state, commands);
            StateInvariants.CheckState(next).ShouldBeEmpty($"The state after turn {state.Turn} violates an invariant.");
            return next;
        }

        /// <summary>
        /// Resolves a turn without any commands.
        /// </summary>
        public GameState PlayEmptyTurn()
        {
            return state.PlayTurn(new Dictionary<string, Dictionary<Hexagon, Command>>());
        }

        /// <summary>
        /// Resolves empty turns until the player has no pending research, at most ten.
        /// </summary>
        public GameState PlayUntilResearchesComplete(string playerId)
        {
            GameState current = state;
            for (int turn = 0; turn < 10 && !current.PlayerStates[playerId].Researches.IsEmpty; turn++)
            {
                current = current.PlayEmptyTurn();
            }
            current.PlayerStates[playerId].Researches.ShouldBeEmpty($"The researches of {playerId} did not complete within ten turns.");
            return current;
        }
    }
}
