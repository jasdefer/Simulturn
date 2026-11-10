using Simulturn.Core.Model.State.Validation;

namespace Simulturn.AI;

/// <summary>
/// Thrown when an artificial player returns commands that do not pass validation.
/// This is always a bug in the player implementation.
/// </summary>
public sealed class InvalidCommandsException : Exception
{
    public string PlayerId { get; }
    public IReadOnlyList<CommandValidation> Validations { get; }

    public InvalidCommandsException(string playerId, IReadOnlyList<CommandValidation> validations)
        : base($"Player '{playerId}' issued invalid commands: {string.Join(", ", validations)}")
    {
        PlayerId = playerId;
        Validations = validations;
    }
}
