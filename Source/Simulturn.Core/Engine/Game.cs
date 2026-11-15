using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;

namespace Simulturn.Core.Engine;

/// <summary>
/// The steps of running a game, for whoever holds the full state: a server, a hot seat client,
/// a runner of AI players. Nothing is kept here. The state of a turn follows from the settings
/// and the commands of all turns before it, so a game is stored by storing its commands.
/// </summary>
public static class Game
{
    /// <summary>
    /// The start of a game: the start state of the settings and an empty report. Hand every
    /// player its view with <see cref="FogOfWar.GetPlayerGameState(TurnResult, string)"/>.
    /// </summary>
    public static TurnResult Start(GameSettings settings)
    {
        return new TurnResult(new GameState(settings), TurnReport.Empty);
    }

    /// <summary>
    /// Plays one turn: validates the commands of every player with <see cref="CommandValidator.Validate"/>
    /// and, if all of them are valid, resolves the turn with <see cref="TurnResolution.Resolve"/>.
    /// A player without an entry gives no orders. Whether the game has ended afterwards tells
    /// <see cref="Standing.Of"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The game is already over.</exception>
    public static TurnAttempt PlayTurn<TCommands>(GameState state, IReadOnlyDictionary<string, TCommands> commands)
        where TCommands : IReadOnlyDictionary<Hexagon, Command>
    {
        if (Standing.Of(state).IsGameOver)
        {
            throw new InvalidOperationException($"The game is over since turn {state.Turn} or earlier, no further turn can be played.");
        }

        var refused = ImmutableDictionary.CreateBuilder<string, ImmutableArray<CommandValidation>>();
        foreach ((string playerId, TCommands playerCommands) in commands)
        {
            ImmutableArray<CommandValidation> validations = [.. CommandValidator.Validate(state, playerId, playerCommands)];
            if (!validations.IsEmpty)
            {
                refused.Add(playerId, validations);
            }
        }
        if (refused.Count > 0)
        {
            return new RefusedCommands(refused.ToImmutable());
        }
        return TurnResolution.Resolve(state, commands);
    }
}
