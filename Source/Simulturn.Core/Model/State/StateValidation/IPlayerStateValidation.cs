namespace Simulturn.Core.Model.State.StateValidation;

internal interface IPlayerStateValidation : IStateValidation
{
    public string PlayerId { get; }
}

public record NegativeUnitCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;
public record NegativeTrainingCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;

public record NegativeBuildingCount(string PlayerId, Hexagon Hexagon, Building Building, short Count) : IPlayerStateValidation;
public record NegativeConstructionCount(string PlayerId, Hexagon Hexagon, Building Building, short ConstructionCount) : IPlayerStateValidation;

/// <summary>
/// The player's level of an upgrade — completed plus in progress — exceeds the number of levels
/// defined in <see cref="GameSettings.Upgrades"/>; applying such a level would read past the
/// upgrade table (e.g. in <see cref="PlayerState.ExponentBonusFromUpgrades"/>).
/// </summary>
public record UpgradeExceedsDefinedLevels(string PlayerId, Upgrade Upgrade, int Level, int DefinedLevels) : IPlayerStateValidation;
