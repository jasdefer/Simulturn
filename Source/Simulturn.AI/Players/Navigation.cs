using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

internal static class Navigation
{
    public static readonly (short X, short Y)[] Directions = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)];

    /// <summary>
    /// The neighbor hexagon that gets closest to the target, with a deterministic tie break.
    /// </summary>
    public static Hexagon StepToward(PlayerGameState view, Hexagon from, Hexagon target)
    {
        return Directions
            .Select(x => new Hexagon((short)(from.X + x.X), (short)(from.Y + x.Y)))
            .Where(view.Observations.ContainsKey)
            .OrderBy(x => x.DistanceTo(target))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .First();
    }
}
