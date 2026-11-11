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
        while (!gameState.IsGameOver && gameState.Turn < maxTurns)
        {
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in gameState.PlayerIds)
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
            onTurnCompleted?.Invoke(turnResult);
        }

        return ToResult(gameState);
    }

    private static GameResult ToResult(GameState gameState)
    {
        List<string> playersWithCompounds = gameState.PlayerStates
            .Where(x => x.Value.Compounds.Any(compound => !compound.Value.IsEmpty))
            .Select(x => x.Key)
            .ToList();
        GameEndReason endReason;
        string? winnerId = null;
        if (!gameState.IsGameOver)
        {
            endReason = GameEndReason.TurnLimit;
        }
        else if (playersWithCompounds.Count == 1)
        {
            endReason = GameEndReason.Victory;
            winnerId = playersWithCompounds[0];
        }
        else
        {
            endReason = GameEndReason.MutualDestruction;
        }
        return new GameResult()
        {
            EndReason = endReason,
            WinnerId = winnerId,
            Turns = gameState.Turn,
            FinalState = gameState
        };
    }
}
