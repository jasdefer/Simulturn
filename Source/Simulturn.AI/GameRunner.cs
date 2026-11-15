using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

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
        GameState gameState = new(gameSettings);
        if (!gameState.PlayerIds.SetEquals(players.Keys))
        {
            throw new ArgumentException($"The players do not match the game settings. " +
                $"Expected: {string.Join(", ", gameState.PlayerIds)}. Provided: {string.Join(", ", players.Keys)}.", nameof(players));
        }

        TurnResult turnResult = new(gameState, TurnReport.Empty);
        GameStanding standing = Standing.Of(gameState);
        while (!standing.IsGameOver && gameState.Turn < maxTurns)
        {
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in gameState.PlayerIds.Except(standing.DefeatedPlayerIds))
            {
                PlayerGameState view = FogOfWar.GetPlayerGameState(turnResult, playerId);
                IReadOnlyDictionary<Hexagon, Command> playerCommands = players[playerId].GetCommands(view);
                var validations = CommandValidator.Validate(gameState, playerId, playerCommands).ToList();
                if (validations.Count > 0)
                {
                    throw new InvalidCommandsException(playerId, validations);
                }
                commands[playerId] = playerCommands;
            }
            turnResult = TurnResolution.Resolve(gameState, commands);
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
