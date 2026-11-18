using Simulturn.Core.Model.State.Validation;

namespace Simulturn.Bots;

/// <summary>
/// A bot issued commands the engine refuses. That is a bug of the bot, not an outcome of a match.
/// </summary>
public sealed class InvalidBotCommandsException(string playerId, ushort turn, ImmutableArray<CommandValidation> validations)
    : Exception($"The bot of {playerId} issued invalid commands on turn {turn}: {string.Join("; ", validations)}")
{
    public string PlayerId { get; } = playerId;

    public ushort Turn { get; } = turn;

    public ImmutableArray<CommandValidation> Validations { get; } = validations;
}
