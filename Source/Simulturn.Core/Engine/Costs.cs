using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

/// <summary>
/// The price of orders. The command validation checks it against the matter and the space of a
/// player, the turn resolution pays it, and a client shows it before the orders are sent: all
/// three ask here, so an upgrade that changes a price is applied here.
/// </summary>
public static class Costs
{
    /// <summary>
    /// The matter one unit of each type costs to train. The only place that reads the prices of
    /// the units from the settings; every other price of a training is computed with it.
    /// </summary>
    public static Army TrainingPrices(GameSettings settings)
    {
        return settings.ArmyCost;
    }

    /// <summary>
    /// The matter one building of each type costs to construct. The only place that reads the
    /// prices of the buildings from the settings; every other price of a construction is computed with it.
    /// </summary>
    public static Compound ConstructionPrices(GameSettings settings)
    {
        return settings.CompoundCost;
    }

    /// <summary>
    /// The price of training the units: their matter and the space they take from the moment they are ordered.
    /// </summary>
    public static OrderCost Training(GameSettings settings, Army training)
    {
        return new OrderCost(training * TrainingPrices(settings), training * settings.RequiredSpace);
    }

    /// <summary>
    /// The price of training a single unit.
    /// </summary>
    public static OrderCost Training(GameSettings settings, Unit unit)
    {
        return Training(settings, Army.FromUnit(unit, 1));
    }

    /// <summary>
    /// The price of constructing the buildings. A construction takes no space.
    /// </summary>
    public static OrderCost Construction(GameSettings settings, Compound construction)
    {
        return new OrderCost(construction * ConstructionPrices(settings), 0);
    }

    /// <summary>
    /// The price of constructing a single building.
    /// </summary>
    public static OrderCost Construction(GameSettings settings, Building building)
    {
        return Construction(settings, Compound.FromBuilding(building, 1));
    }

    /// <summary>
    /// The price of researching the next level of the upgrade for a player with the given
    /// completed levels. An upgrade without a further level costs nothing; the command
    /// validation refuses such a research.
    /// </summary>
    public static OrderCost Research(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels, Upgrade upgrade)
    {
        return new OrderCost(settings.NextUpgradeLevel(upgrade, upgradeLevels.GetValueOrDefault(upgrade))?.Cost ?? 0, 0);
    }

    /// <summary>
    /// The price of the orders on one hexagon. Movements are free.
    /// </summary>
    public static OrderCost Of(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels, Command command)
    {
        OrderCost cost = Training(settings, command.Training) + Construction(settings, command.Construction);
        return command.Research is Upgrade upgrade
            ? cost + Research(settings, upgradeLevels, upgrade)
            : cost;
    }

    /// <summary>
    /// The price of all orders of a player in one turn: the sum over the hexagons.
    /// </summary>
    public static OrderCost Of(GameSettings settings, IReadOnlyDictionary<Upgrade, byte> upgradeLevels, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        OrderCost cost = default;
        foreach (Command command in commands.Values)
        {
            cost += Of(settings, upgradeLevels, command);
        }
        return cost;
    }

    /// <summary>
    /// What the orders of a turn leave a player to order with: the matter after paying them and
    /// the space after the units in them. Negative matter means the orders cost more than the
    /// player has. Negative space only breaks a rule when the orders train a unit: a player may
    /// hold more units than the buildings give space for, but trains none then.
    /// </summary>
    public static OrderCost Budget(GameSettings settings, PlayerState playerState, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        OrderCost cost = Of(settings, playerState.UpgradeLevels, commands);
        return new OrderCost(playerState.Matter - cost.Matter, playerState.AvailableSpace - playerState.UsedSpace - cost.Space);
    }
}
