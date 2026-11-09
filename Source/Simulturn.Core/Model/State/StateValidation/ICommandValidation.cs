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

/// <summary>
/// The researches requested this turn would push the completed and pending levels of the upgrade
/// beyond the levels defined in <see cref="GameSettings.Upgrades"/>. Reported for every research
/// command of that upgrade, so the result does not depend on the order of the commands.
/// </summary>
public record UpgradeExceedsAvailableLevel(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte CompletedLevels, byte PendingLevels, byte RequestedLevels, int MaxLevel) : ICommandValidation;

public record UpgradeNotResearchableOnHexagon(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;

public record UpgradeRequiresDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;