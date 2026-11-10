using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

internal static class Navigation
{
    /// <summary>
    /// The neighbor hexagon that gets closest to the target, with a deterministic tie break.
    /// </summary>
    public static Hexagon StepToward(PlayerGameState view, Hexagon from, Hexagon target)
    {
        return from.Neighbors()
            .Where(view.Observations.ContainsKey)
            .OrderBy(x => x.DistanceTo(target))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .First();
    }
}
