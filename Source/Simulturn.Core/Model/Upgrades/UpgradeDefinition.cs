namespace Simulturn.Core.Model.Upgrades;

/// <summary>
/// One upgrade of the rules: the unit types it acts on and its levels. An upgrade belongs to a
/// player, not to a single unit, and acts on every unit of the named types.
/// </summary>
public sealed record UpgradeDefinition
{
    /// <summary>
    /// The unit types the upgrade acts on. A simple effect reads this list, so the two cannot
    /// contradict each other. An opponent who recognizes one of these types in an army of the
    /// player learns the level of the upgrade, see <see cref="Engine.UpgradeRules.ActingOn"/>.
    /// </summary>
    public required ImmutableArray<Unit> AffectedUnits { get; init; }

    /// <summary>
    /// The levels in the order they are researched. A level of 1 is the first entry, at index 0.
    /// </summary>
    public required ImmutableArray<UpgradeLevel> Levels { get; init; }
}
