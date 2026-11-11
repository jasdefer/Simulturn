using Simulturn.Core.Model;

namespace Simulturn.Core.Engine;

/// <summary>
/// The fight rule. Knows only armies and exponents, not who owns them or where they stand.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Resolves a fight between two armies and returns the losses of both sides.
    /// The stronger army destroys the weaker one and loses the fraction of itself that matches
    /// the strength ratio, rounded up per unit. Two armies without any strength destroy each other.
    /// </summary>
    /// <param name="exponent1">Fight exponent of the first army, upgrade bonus included.</param>
    /// <param name="exponent2">Fight exponent of the second army, upgrade bonus included.</param>
    public static (Army Losses1, Army Losses2) Fight(Army army1, Army exponent1, Army army2, Army exponent2)
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
