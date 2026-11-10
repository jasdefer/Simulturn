namespace Simulturn.Core.Model.State.Validation;

/// <summary>
/// A violated invariant found by <see cref="GameState.IsValid"/>. The cases are the closed set of
/// state rules, so a <c>switch</c> over a validation is exhaustive. Some cases are shared with
/// <see cref="CommandValidation"/>: a state can be invalid for the same reason a command is refused.
/// </summary>
public union StateValidation(
    NegativeRemainingMatter,
    HexagonMissingInSettings,
    InvalidPlayerId,
    NegativeUnitCount,
    NegativeTrainingCount,
    NegativeBuildingCount,
    NegativeConstructionCount,
    ExpiredTraining,
    ExpiredConstruction,
    ExpiredResearch,
    MissingBuildingForTraining,
    MissingDotsForConstruction,
    UpgradeNotResearchableOnHexagon,
    ResearchRequiresDot,
    UpgradeLevelsExceedTable,
    UpgradeInResearchOnMultipleHexagons)
{
    public override string ToString() => Value?.ToString() ?? nameof(StateValidation);
}

public sealed record NegativeRemainingMatter(Hexagon Hexagon, int RemainingMatter);

public sealed record HexagonMissingInSettings(Hexagon Hexagon);

public sealed record NegativeUnitCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count);
public sealed record NegativeTrainingCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count);

public sealed record NegativeBuildingCount(string PlayerId, Hexagon Hexagon, Building Building, short Count);
public sealed record NegativeConstructionCount(string PlayerId, Hexagon Hexagon, Building Building, short ConstructionCount);

/// <summary>
/// An entry whose completion turn lies in the past. The queues only hold pending work:
/// <see cref="GameState.NextTurn"/> removes every entry in the turn it is applied.
/// </summary>
public sealed record ExpiredTraining(string PlayerId, ushort CompletionTurn);
public sealed record ExpiredConstruction(string PlayerId, ushort CompletionTurn);
public sealed record ExpiredResearch(string PlayerId, ushort CompletionTurn);

/// <summary>
/// The completed and pending levels of an upgrade exceed the levels defined in
/// <see cref="GameSettings.Upgrades"/>. Such a state cannot be reached through validated commands.
/// </summary>
public sealed record UpgradeLevelsExceedTable(string PlayerId, Upgrade Upgrade, byte CompletedLevels, byte PendingLevels, int MaxLevel);

/// <summary>
/// The upgrade is pending on more than one hexagon. Researches never stack, so such a state cannot
/// be reached through validated commands.
/// </summary>
public sealed record UpgradeInResearchOnMultipleHexagons(string PlayerId, Upgrade Upgrade, byte PendingCount);
