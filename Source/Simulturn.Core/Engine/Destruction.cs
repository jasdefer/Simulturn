using Simulturn.Core.Model;

namespace Simulturn.Core.Engine;

/// <summary>
/// The rule of destroying buildings. Knows only an army and the buildings it stands on. The turn
/// resolution calls it for the armies left after the fights, a client that forecasts a raid
/// passes the army it expects to arrive.
/// </summary>
public static class Destruction
{
    /// <summary>
    /// The buildings of the compound the army destroys in one turn. Every unit adds its structure
    /// damage. The damage is spent on the buildings with the lowest armor first, a building falls
    /// only when its whole armor is paid, and what is left over is lost.
    /// </summary>
    public static Compound Of(GameSettings settings, Army army, Compound compound)
    {
        double damage = settings.StructureDamage * army;
        Compound destroyed = Compound.Empty;
        foreach (Building building in EnumValues.Buildings.OrderBy(x => settings.Armor[x]))
        {
            short count = (short)Math.Min(compound[building], Math.Floor(damage / (double)settings.Armor[building]));
            if (count <= 0)
            {
                continue;
            }
            damage -= count * settings.Armor[building];
            destroyed += Compound.FromBuilding(building, count);
        }
        return destroyed;
    }
}
