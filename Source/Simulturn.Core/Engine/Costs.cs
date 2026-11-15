using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;

namespace Simulturn.Core.Engine;

/// <summary>
/// The price of orders. The command validation checks it against the matter and the space of a
/// player, the turn resolution pays it, and a client shows it before the orders are sent: all
/// three ask here, so an upgrade that changes a price is applied here.
/// </summary>
public static class Costs
{
    /// <summary>
    /// The price of training the units: their matter and the space they take from the moment they are ordered.
    /// </summary>
    public static OrderCost Training(GameSettings settings, Army training)
    {
        return new OrderCost(training * settings.ArmyCost, training * settings.RequiredSpace);
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
        return new OrderCost(construction * settings.CompoundCost, 0);
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
}
