using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using System.Collections.Immutable;

namespace Simulturn.AI;

/// <summary>
/// Plays a full game between artificial players.
/// The runner acts as the server: it holds the full game state and hands each player
/// only its partially informed view.
/// </summary>
public static class GameRunner
{
    public const ushort DefaultMaxTurns = 500;

    /// <param name="maxTurns">
    /// The game ends in a draw after this many turns. Required because a game between
    /// passive players never destroys all buildings and would run forever.
    /// </param>
    /// <param name="onTurnCompleted">
    /// Optional callback after every resolved turn with the all-knowing state and report, e.g. for logging or replays.
    /// </param>
    public static GameResult Run(GameSettings gameSettings,
        IReadOnlyDictionary<string, IArtificialPlayer> players,
        ushort maxTurns = DefaultMaxTurns,
        Action<TurnResult>? onTurnCompleted = null)
    {
        TurnResult turnResult = Game.Start(gameSettings);
        GameState gameState = turnResult.State;
        if (!gameState.PlayerIds.SetEquals(players.Keys))
        {
            throw new ArgumentException($"The players do not match the game settings. " +
                $"Expected: {string.Join(", ", gameState.PlayerIds)}. Provided: {string.Join(", ", players.Keys)}.", nameof(players));
        }

        GameStanding standing = Standing.Of(gameState);
        while (!standing.IsGameOver && gameState.Turn < maxTurns)
        {
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in gameState.PlayerIds.Except(standing.DefeatedPlayerIds))
            {
                PlayerGameState view = FogOfWar.GetPlayerGameState(turnResult, playerId);
                commands[playerId] = players[playerId].GetCommands(view);
            }
            switch (Game.PlayTurn(gameState, commands))
            {
                case RefusedCommands refused:
                    (string playerId, ImmutableArray<CommandValidation> validations) = refused.Validations.OrderBy(x => x.Key, StringComparer.Ordinal).First();
                    throw new InvalidCommandsException(playerId, validations);
                case TurnResult resolved:
                    turnResult = resolved;
                    break;
            }
            gameState = turnResult.State;
            standing = Standing.Of(gameState);
            onTurnCompleted?.Invoke(turnResult);
        }

        return new GameResult()
        {
            EndReason = standing.Outcome switch
            {
                GameRunning => GameEndReason.TurnLimit,
                Victory => GameEndReason.Victory,
                MutualDefeat => GameEndReason.MutualDestruction
            },
            WinnerId = standing.Outcome.Value is Victory victory ? victory.WinnerId : null,
            Turns = gameState.Turn,
            FinalState = gameState
        };
    }
}
