namespace Simulturn.Core.Model.State.StateValidation;

public interface ICommandValidation
{
    string PlayerId { get; }
}

public sealed record InvalidPlayerId(string PlayerId) : ICommandValidation, IPlayerStateValidation;

public sealed record InsufficientMatter(string PlayerId, int Required, int Available) : ICommandValidation;

public sealed record InsufficientSpace(string PlayerId, int Required, int Available) : ICommandValidation;

public sealed record MissingBuildingForTraining(string PlayerId, Hexagon Hexagon, Unit Unit, short TrainedCount, short BuildingCount) : ICommandValidation, IPlayerStateValidation;

public sealed record MissingDotsForConstruction(string PlayerId, Hexagon Hexagon, Compound Constructions, short AvailableDots) : ICommandValidation, IPlayerStateValidation;

public sealed record MissingArmyForMovement(string PlayerId, Hexagon Hexagon, Unit Unit, short MovingUnits, short AvailableUnits) : ICommandValidation;

public sealed record UpgradeExceedsAvailableLevel(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte CurrentLevel, int AvailableLevel) : ICommandValidation, IPlayerStateValidation;

public sealed record UpgradeRequiresDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;