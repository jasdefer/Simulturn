namespace Simulturn.Core.Model.State.Validation;

/// <summary>
/// A reason why <see cref="CommandValidator.Validate"/> refuses the commands of a player. The cases are
/// the closed set of command rules, so a <c>switch</c> over a validation is exhaustive.
/// </summary>
public union CommandValidation(
    InvalidPlayerId,
    PlayerDefeated,
    InsufficientMatter,
    InsufficientSpace,
    MissingBuildingForTraining,
    MissingDotsForConstruction,
    HexagonNotBuildable,
    MissingArmyForMovement,
    MovementExceedsRange,
    MovementDestinationOffMap,
    UpgradeExceedsAvailableLevel,
    UpgradeAlreadyInResearch,
    UpgradeRequestedOnMultipleHexagons,
    HexagonAlreadyResearching,
    UpgradeNotResearchableOnHexagon,
    ResearchRequiresDot,
    MovementLeavesResearchWithoutDot,
    MovementLeavesConstructionWithoutDot)
{
    public override string ToString() => Value?.ToString() ?? nameof(CommandValidation);
}

public sealed record InvalidPlayerId(string PlayerId);

/// <summary>
/// The player is defeated (<see cref="Standing.IsDefeated(PlayerState)"/>) and takes no more orders.
/// </summary>
public sealed record PlayerDefeated(string PlayerId);

public sealed record InsufficientMatter(string PlayerId, int Required, int Available);

public sealed record InsufficientSpace(string PlayerId, int Required, int Available);

public sealed record MissingBuildingForTraining(string PlayerId, Hexagon Hexagon, Unit Unit, short TrainedCount, short BuildingCount);

public sealed record MissingDotsForConstruction(string PlayerId, Hexagon Hexagon, Compound Constructions, short AvailableDots);

/// <summary>
/// A construction is ordered on a hexagon whose <see cref="HexagonSettings.IsBuildable"/> is false.
/// </summary>
public sealed record HexagonNotBuildable(string PlayerId, Hexagon Hexagon);

public sealed record MissingArmyForMovement(string PlayerId, Hexagon Hexagon, Unit Unit, short MovingUnits, short AvailableUnits);

/// <summary>
/// The destination is further away than the unit type moves in one turn, see <see cref="Movement"/>.
/// Reported for every unit type of the movement that does not get there.
/// </summary>
public sealed record MovementExceedsRange(string PlayerId, Hexagon Hexagon, Hexagon Destination, Unit Unit, int Distance, short Range);

/// <summary>
/// The destination is no hexagon of the map.
/// </summary>
public sealed record MovementDestinationOffMap(string PlayerId, Hexagon Hexagon, Hexagon Destination);

/// <summary>
/// Every level of the upgrade defined in <see cref="GameSettings.Upgrades"/> is already completed.
/// </summary>
public sealed record UpgradeExceedsAvailableLevel(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte CompletedLevels, int MaxLevel);

/// <summary>
/// A research of the upgrade is pending. The next level can only be started once it has completed;
/// researches never stack, on no hexagon.
/// </summary>
public sealed record UpgradeAlreadyInResearch(string PlayerId, Hexagon Hexagon, Upgrade Upgrade);

/// <summary>
/// The upgrade is requested on more than one hexagon this turn. Reported for every research command
/// of that upgrade, so the result does not depend on the order of the commands.
/// </summary>
public sealed record UpgradeRequestedOnMultipleHexagons(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, byte RequestedCount);

/// <summary>
/// The hexagon hosts a pending research. A hexagon researches one upgrade at a time.
/// </summary>
public sealed record HexagonAlreadyResearching(string PlayerId, Hexagon Hexagon, Upgrade Upgrade, Upgrade UpgradeInResearch);

public sealed record UpgradeNotResearchableOnHexagon(string PlayerId, Hexagon Hexagon, Upgrade Upgrade);

public sealed record ResearchRequiresDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade);

/// <summary>
/// The movements take the last dot off a hexagon with a pending or requested research. A research
/// needs a dot on its hexagon until it completes; without one it is canceled and the matter is lost,
/// so leaving voluntarily is refused.
/// </summary>
public sealed record MovementLeavesResearchWithoutDot(string PlayerId, Hexagon Hexagon, Upgrade Upgrade);

/// <summary>
/// The movements take a dot off a hexagon that a pending or requested construction needs. Every
/// building under construction keeps its own dot on the hexagon until it completes. A dot arriving
/// in the same turn does not stand in: the dot that builds stays.
/// </summary>
public sealed record MovementLeavesConstructionWithoutDot(string PlayerId, Hexagon Hexagon, Compound Constructions, short RemainingDots);
