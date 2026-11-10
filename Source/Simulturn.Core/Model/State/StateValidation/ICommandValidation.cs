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

/// <summary>
/// Every level of the upgrade defined in <see cref="GameSettings.Upgrades"/> is already completed.
/// </summary>
public sealed record UpgradeExceedsAvailableLevel(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte CompletedLevels, int MaxLevel) : ICommandValidation;

/// <summary>
/// A research of the upgrade is pending. The next level can only be started once it has completed;
/// researches never stack, on no hexagon.
/// </summary>
public sealed record UpgradeAlreadyInResearch(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation;

/// <summary>
/// The upgrade is requested on more than one hexagon this turn. Reported for every research command
/// of that upgrade, so the result does not depend on the order of the commands.
/// </summary>
public sealed record UpgradeRequestedOnMultipleHexagons(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte RequestedCount) : ICommandValidation;

/// <summary>
/// The hexagon hosts a pending research. A hexagon researches one upgrade at a time.
/// </summary>
public sealed record HexagonAlreadyResearching(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, Upgrade UpgradeInResearch) : ICommandValidation;

public sealed record UpgradeNotResearchableOnHexagon(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;

public sealed record ResearchRequiresDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation, IPlayerStateValidation;

/// <summary>
/// The movements take the last dot off a hexagon with a pending or requested research. A research
/// needs a dot on its hexagon until it completes; without one it is canceled and the matter is lost,
/// so leaving voluntarily is refused.
/// </summary>
public sealed record MovementLeavesResearchWithoutDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade) : ICommandValidation;
