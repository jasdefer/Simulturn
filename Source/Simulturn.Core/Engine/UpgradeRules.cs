using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Engine;

/// <summary>
/// What follows from the upgrade levels of a player, independent of the rule an upgrade changes.
/// </summary>
public static class UpgradeRules
{
    /// <summary>
    /// The effects of the upgrades a player has, each with the definition of its upgrade. Only
    /// the level a player has counts: the effect of a level is absolute and replaces the levels
    /// before it. A rule picks the kind of effect it applies and ignores the others.
    /// </summary>
    public static IEnumerable<(UpgradeDefinition Definition, UpgradeEffect Effect)> Effects(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels)
    {
        foreach ((Upgrade upgrade, byte level) in upgradeLevels)
        {
            if (level == 0)
            {
                continue;
            }
            UpgradeDefinition definition = settings.Upgrades[upgrade];
            // A level of 1 means the first level, which is at index 0.
            yield return (definition, definition.Levels[level - 1].Effect);
        }
    }

    /// <summary>
    /// The upgrade levels a player starts the game with: every entry of
    /// <see cref="GameSettings.StartUpgrades"/> is one level. The settings are known to every
    /// player, so the start upgrades of an opponent are no secret.
    /// </summary>
    public static ImmutableDictionary<Upgrade, byte> StartLevels(GameSettings settings, string playerId)
    {
        if (!settings.StartUpgrades.TryGetValue(playerId, out ImmutableArray<Upgrade> upgrades))
        {
            return ImmutableDictionary<Upgrade, byte>.Empty;
        }
        return upgrades.GroupBy(x => x).ToImmutableDictionary(x => x.Key, x => Convert.ToByte(x.Count()));
    }

    /// <summary>
    /// The matter a player with the given upgrade levels starts the game with: the start matter
    /// of the settings plus every <see cref="StartMatterBonus"/>. Only the start state asks here;
    /// a level researched during the game comes too late to change anything.
    /// </summary>
    public static int StartMatter(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels)
    {
        int matter = settings.StartMatter;
        foreach ((_, UpgradeEffect effect) in Effects(settings, upgradeLevels))
        {
            if (effect.Value is StartMatterBonus bonus)
            {
                matter += bonus.Matter;
            }
        }
        return matter;
    }

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
