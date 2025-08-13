namespace Simulturn.Core.Model.State.StateValidation;
internal interface IStateValidation
{
    string PlayerId { get; }
}

public record InvalidPlayerId(string PlayerId) : IStateValidation;

public record InsufficientMatter(string PlayerId, int Required, int Available) : IStateValidation;

public record InsufficientSpace(string PlayerId, int Required, int Available) : IStateValidation;