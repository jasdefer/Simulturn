using Simulturn.Core.Model;

namespace Simulturn.Core.Engine;

/// <summary>
/// The movement rule: how far every unit type gets in one turn. The distance is the hexagon distance
/// between origin and destination; what lies in between plays no role, so an army passes other armies
/// and gaps in the map. <see cref="CommandValidator"/> and a client that offers destinations both ask here.
/// </summary>
public static class Movement
{
    /// <summary>
    /// The number of hexagons every unit type moves per turn.
    /// </summary>
    public static Army Range(GameSettings settings)
    {
        return settings.MovementRange;
    }

    /// <summary>
    /// The part of the army whose unit types get as far as the distance. This is the only place
    /// that compares a range with a distance.
    /// </summary>
    public static Army ReachableArmy(GameSettings settings, Army army, int distance)
    {
        Army range = Range(settings);
        Army reachable = army;
        foreach (Unit unit in EnumValues.Units)
        {
            if (range[unit] < distance)
            {
                reachable = reachable.AddUnit(unit, (short)-army[unit]);
            }
        }
        return reachable;
    }

    /// <summary>
    /// Every hexagon of the map that at least one unit of the army reaches from the origin, with the
    /// part of the army that gets there. The whole army reaches a hexagon when the part equals the army.
    /// The origin itself is no destination.
    /// </summary>
    public static ImmutableDictionary<Hexagon, Army> Destinations(GameSettings settings, Hexagon origin, Army army)
    {
        var destinations = ImmutableDictionary.CreateBuilder<Hexagon, Army>();
        foreach (Hexagon hexagon in settings.HexagonSettings.Keys)
        {
            if (hexagon == origin)
            {
                continue;
            }
            Army reachable = ReachableArmy(settings, army, origin.DistanceTo(hexagon));
            if (!reachable.IsEmpty)
            {
                destinations.Add(hexagon, reachable);
            }
        }
        return destinations.ToImmutable();
    }
}
