using Simulturn.Core.Model.State.StateValidation;

namespace Simulturn.AI;

/// <summary>
/// Thrown when an artificial player returns commands that do not pass validation.
/// This is always a bug in the player implementation.
/// </summary>
public class InvalidCommandsException : Exception
{
    public string PlayerId { get; }
    public IReadOnlyList<ICommandValidation> Validations { get; }

    public InvalidCommandsException(string playerId, IReadOnlyList<ICommandValidation> validations)
        : base($"Player '{playerId}' issued invalid commands: {string.Join(", ", validations)}")
    {
        PlayerId = playerId;
        Validations = validations;
    }
}
