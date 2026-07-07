using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.Web.Services;

/// <summary>
/// Legal movement destinations from a source hex, based on the per-unit-type
/// <see cref="GameSettings.MovementRange"/>. Terrain influence is a future engine feature
/// and would slot in here.
/// </summary>
public static class MovementRange
{
    /// <summary>
    /// Hexagons reachable by at least one of the given units (the fastest available unit
    /// determines the highlighted radius; per-unit limits are enforced in the stepper).
    /// </summary>
    public static IReadOnlySet<Hexagon> GetDestinations(GameState state, Hexagon source, Army availableArmy)
    {
        int maxRange = MaxRange(state.GameSettings, availableArmy);
        return state.Hexagons
            .Where(hexagon => hexagon != source && source.DistanceTo(hexagon) <= maxRange)
            .ToHashSet();
    }

    /// <summary>The available units clamped to those able to cover the given distance.</summary>
    public static Army ReachableArmy(GameSettings settings, Army availableArmy, int distance)
    {
        Army reachable = availableArmy;
        foreach (var unit in Enum.GetValues<Unit>())
        {
            if (settings.MovementRange[unit] < distance && availableArmy[unit] > 0)
            {
                reachable = reachable.AddUnit(unit, (short)-availableArmy[unit]);
            }
        }
        return reachable;
    }

    private static int MaxRange(GameSettings settings, Army availableArmy)
    {
        int maxRange = 0;
        foreach (var unit in Enum.GetValues<Unit>())
        {
            if (availableArmy[unit] > 0)
            {
                maxRange = Math.Max(maxRange, settings.MovementRange[unit]);
            }
        }
        return maxRange;
    }
}
