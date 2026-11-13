using Simulturn.Core.Model;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Engine;

/// <summary>
/// The fight rule. Knows only armies and upgrade levels, not where the armies stand.
/// The turn resolution and a forecast of a client call the same method, they differ only in what they pass.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Resolves the fight of all armies on one hexagon, keyed by player id.
    /// Every army fights every other army with its full strength. The losses of an army add up
    /// over its fights and end at the army itself: only the strongest army can survive, and only
    /// if the other armies together do not wear it down.
    /// </summary>
    public static FightReport Fight(GameSettings settings, IReadOnlyDictionary<string, Combatant> combatants)
    {
        List<(string PlayerId, Army Army, Army Exponent)> sides = [];
        foreach ((string playerId, Combatant combatant) in combatants)
        {
            sides.Add((playerId, combatant.Army, GetExponent(settings, combatant.UpgradeLevels)));
        }

        Dictionary<string, Army> losses = sides.ToDictionary(x => x.PlayerId, _ => Army.Empty);
        for (int i = 0; i < sides.Count - 1; i++)
        {
            for (int j = i + 1; j < sides.Count; j++)
            {
                (Army lossesI, Army lossesJ) = Fight(sides[i].Army, sides[i].Exponent, sides[j].Army, sides[j].Exponent);
                losses[sides[i].PlayerId] += lossesI;
                losses[sides[j].PlayerId] += lossesJ;
            }
        }

        return new FightReport()
        {
            Armies = sides.ToImmutableDictionary(x => x.PlayerId, x => x.Army),
            Losses = sides.ToImmutableDictionary(x => x.PlayerId, x => Army.Min(x.Army, losses[x.PlayerId]))
        };
    }

    /// <summary>
    /// The fight exponent of an army whose player has the given upgrade levels: the exponent of
    /// the settings plus the bonus of the upgrades.
    /// </summary>
    public static Army GetExponent(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels)
    {
        // TODO: Extend upgrade exponent bonuses to support all unit-specific upgrades.
        Army exponent = settings.FightExponent;
        if (upgradeLevels.TryGetValue(Upgrade.DotUpgrade, out byte dotUpgradeLevel) && dotUpgradeLevel > 0)
        {
            // A level of 1 means the first upgrade, which is at index 0.
            exponent = exponent.AddUnit(Unit.Dot, ((DotUpgrade)settings.Upgrades[Upgrade.DotUpgrade][dotUpgradeLevel - 1]).ExponentBonus);
        }
        return exponent;
    }

    /// <summary>
    /// Resolves a fight between two armies and returns the losses of both sides.
    /// The stronger army destroys the weaker one and loses the fraction of itself that matches
    /// the strength ratio, rounded up per unit. Two armies of the same strength destroy each other.
    /// </summary>
    private static (Army Losses1, Army Losses2) Fight(Army army1, Army exponent1, Army army2, Army exponent2)
    {
        double strength1 = army1.GetStrengthOver(army2, exponent1);
        double strength2 = army2.GetStrengthOver(army1, exponent2);
        if (strength1 > strength2)
        {
            return (army1.MultiplyAndRoundUp(strength2 / strength1), army2);
        }
        if (strength1 == 0 && strength2 == 0)
        {
            return (army1, army2);
        }
        return (army1, army2.MultiplyAndRoundUp(strength1 / strength2));
    }
}
