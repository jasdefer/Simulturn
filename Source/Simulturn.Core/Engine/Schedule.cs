using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Engine;

/// <summary>
/// When an order completes. The turn it is ordered in is the first turn of its duration, so an
/// order with a duration of one completes in the turn it is ordered. The turn resolution queues
/// the orders with it, a client tells with it when an order will be done.
/// </summary>
public static class Schedule
{
    /// <summary>
    /// The turn in which an order with the duration completes when it is ordered in the given turn.
    /// </summary>
    public static ushort CompletionTurn(ushort turn, int duration)
    {
        return (ushort)(turn + duration - 1);
    }

    /// <summary>
    /// The turn in which a unit completes its training when it is ordered in the given turn.
    /// </summary>
    public static ushort TrainingCompletion(GameSettings settings, ushort turn, Unit unit)
    {
        return CompletionTurn(turn, settings.TrainingDuration[unit]);
    }

    /// <summary>
    /// The turn in which a building completes its construction when it is ordered in the given turn.
    /// </summary>
    public static ushort ConstructionCompletion(GameSettings settings, ushort turn, Building building)
    {
        return CompletionTurn(turn, settings.ConstructionDuration[building]);
    }

    /// <summary>
    /// The turn in which the research of the level completes when it is ordered in the given turn.
    /// The level is the one <see cref="GameSettings.NextUpgradeLevel"/> gives.
    /// </summary>
    public static ushort ResearchCompletion(ushort turn, UpgradeLevel level)
    {
        return CompletionTurn(turn, level.Duration);
    }
}
