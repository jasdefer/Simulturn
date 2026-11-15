namespace Simulturn.Core.Model.Upgrades;

/// <summary>
/// One level of an upgrade: what its research costs and how long it takes, and what the upgrade
/// does once the level is completed.
/// </summary>
/// <param name="Cost">The matter the research of this level costs.</param>
/// <param name="Duration">The number of turns the research of this level takes.</param>
/// <param name="Effect">
/// The effect of the upgrade at this level. The values are absolute: a level replaces the
/// effect of the level before, nothing adds up over the levels.
/// </param>
public sealed record UpgradeLevel(short Cost, byte Duration, UpgradeEffect Effect);
