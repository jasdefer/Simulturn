namespace Simulturn.Core.Model.State.StateValidation;

public interface ICommandValidation
{
    string PlayerId { get; }
}

public record InvalidPlayerId(string PlayerId) : ICommandValidation, IPlayerStateValidation;

public record InsufficientMatter(string PlayerId, int Required, int Available) : ICommandValidation;

public record InsufficientSpace(string PlayerId, int Required, int Available) : ICommandValidation;

public record MissingBuildingForTraining(string PlayerId, Hexagon Hexagon, Unit Unit, short TrainedCount, short BuildingCount) : ICommandValidation, IPlayerStateValidation;

public record MissingDotsForConstruction(string PlayerId, Hexagon Hexagon, Compound Constructions, short AvailableDots) : ICommandValidation, IPlayerStateValidation;

public record MissingArmyForMovement(string PlayerId, Hexagon Hexagon, Unit Unit, short MovingUnits, short AvailableUnits) : ICommandValidation;

public record MovementExceedsRange(string PlayerId, Hexagon Hexagon, Hexagon Destination, Unit Unit, int Distance, short Range) : ICommandValidation;

/// <summary>
/// <paramref name="CurrentLevel"/> counts completed levels plus researches still in progress or
/// commanded in the same turn; <paramref name="AvailableLevel"/> is the lower of the hexagon's
/// researchable levels and the levels defined in <see cref="GameSettings.Upgrades"/>.
/// </summary>
public record UpgradeExceedsAvailableLevel(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, int CurrentLevel, int AvailableLevel) : ICommandValidation, IPlayerStateValidation;

public record UpgradeRequiresDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;