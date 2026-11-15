using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Engine;

/// <summary>
/// What follows from the upgrade levels of a player, independent of the rule an upgrade changes.
/// </summary>
public static class UpgradeRules
{
    /// <summary>
    /// The upgrades of a player that act on an army: every researched upgrade with at least one
    /// of its unit types in the army, with its level. This is what somebody learns who recognizes
    /// the composition of the army, and all a fight of the army depends on.
    /// </summary>
    public static ImmutableDictionary<Upgrade, byte> ActingOn(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels, Army army)
    {
        var acting = ImmutableDictionary.CreateBuilder<Upgrade, byte>();
        foreach ((Upgrade upgrade, byte level) in upgradeLevels)
        {
            if (level > 0 && settings.Upgrades[upgrade].AffectedUnits.Any(unit => army[unit] > 0))
            {
                acting.Add(upgrade, level);
            }
        }
        return acting.ToImmutable();
    }
}
