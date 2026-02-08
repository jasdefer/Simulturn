namespace Simulturn.Core.Model.State.StateValidation;

internal interface IPlayerStateValidation : IStateValidation
{
    public string PlayerId { get; }
}

public record NegativeUnitCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;
public record NegativeTrainingCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;

public record NegativeBuildingCount(string PlayerId, Hexagon Hexagon, Building Building, short Count) : IPlayerStateValidation;
public record NegativeConstructionCount(string PlayerId, Hexagon Hexagon, Building Building, short ConstructionCount) : IPlayerStateValidation;
