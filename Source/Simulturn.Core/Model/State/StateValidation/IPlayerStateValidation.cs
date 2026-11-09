namespace Simulturn.Core.Model.State.StateValidation;

internal interface IPlayerStateValidation : IStateValidation
{
    public string PlayerId { get; }
}

public sealed record NegativeUnitCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;
public sealed record NegativeTrainingCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;

public sealed record NegativeBuildingCount(string PlayerId, Hexagon Hexagon, Building Building, short Count) : IPlayerStateValidation;
public sealed record NegativeConstructionCount(string PlayerId, Hexagon Hexagon, Building Building, short ConstructionCount) : IPlayerStateValidation;

/// <summary>
/// An entry whose completion turn lies in the past. The queues only hold pending work:
/// <see cref="GameState.NextTurn"/> removes every entry in the turn it is applied.
/// </summary>
public sealed record ExpiredTraining(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;
public sealed record ExpiredConstruction(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;
public sealed record ExpiredResearch(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;
