using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Test.Common;

/// <summary>
/// The promise of <see cref="Orders.Possible"/>, checked against <see cref="CommandValidator.Validate"/>:
/// every order is accepted up to its limit and refused beyond it. What is offered and what is
/// valid can therefore never drift apart unnoticed. Returns human readable violations.
/// </summary>
public static class OrderInvariants
{
    private static readonly PossibleHexagonOrders _nothing = new(Army.Empty, Compound.Empty, [], Army.Empty);

    /// <summary>
    /// Checks every order on every hexagon the player has an army or a building on, with the
    /// valid commands entered so far.
    /// </summary>
    public static IReadOnlyList<string> Check(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        List<string> violations = [];
        GameSettings settings = view.GameSettings;
        PlayerState player = view.PlayerState;
        PossibleOrders possible = Orders.Possible(view, commands);

        OrderCost cost = Costs.Of(settings, player.UpgradeLevels, commands);
        OrderCost expectedBudget = new(player.Matter - cost.Matter, player.AvailableSpace - player.UsedSpace - cost.Space);
        if (possible.Budget != expectedBudget)
        {
            violations.Add($"{view.PlayerId}: the budget is {possible.Budget}, but the state and the commands leave {expectedBudget}.");
        }

        IEnumerable<Hexagon> hexagons = player.Armies.Keys.Union(player.Compounds.Keys).OrderBy(x => x.X).ThenBy(x => x.Y);
        foreach (Hexagon hexagon in hexagons)
        {
            PossibleHexagonOrders orders = possible.Hexagons.GetValueOrDefault(hexagon, _nothing);
            foreach (Unit unit in Enum.GetValues<Unit>())
            {
                CheckLimit(violations, view, $"training {unit} on {hexagon}", orders.Training[unit],
                    count => new CommandsBuilder(commands).Train(hexagon, Army.FromUnit(unit, count)).Commands);
            }
            foreach (Building building in Enum.GetValues<Building>())
            {
                CheckLimit(violations, view, $"constructing {building} on {hexagon}", orders.Construction[building],
                    count => new CommandsBuilder(commands).Construct(hexagon, Compound.FromBuilding(building, count)).Commands);
            }
            CheckResearch(violations, view, commands, hexagon, orders);
            foreach (Unit unit in Enum.GetValues<Unit>())
            {
                // Any destination in range will do: the limit of a movement does not depend on it.
                var destinations = Movement.Destinations(settings, hexagon, Army.FromUnit(unit, 1)).Keys
                    .OrderBy(x => x.X).ThenBy(x => x.Y)
                    .ToList();
                if (destinations.Count == 0)
                {
                    continue;
                }
                CheckLimit(violations, view, $"moving {unit} off {hexagon}", orders.Movable[unit],
                    count => new CommandsBuilder(commands).Move(hexagon, destinations[0], Army.FromUnit(unit, count)).Commands);
            }
        }
        return violations;
    }

    private static void CheckLimit(List<string> violations, PlayerGameState view, string order, short limit,
        Func<short, IReadOnlyDictionary<Hexagon, Command>> withOrder)
    {
        if (limit < 0)
        {
            violations.Add($"{view.PlayerId}: {order} has the negative limit {limit}.");
            return;
        }
        if (limit > 0 && !IsValid(view, withOrder(limit)))
        {
            violations.Add($"{view.PlayerId}: {order} is offered {limit} times, but the validation refuses that.");
        }
        if (IsValid(view, withOrder((short)(limit + 1))))
        {
            violations.Add($"{view.PlayerId}: {order} is offered {limit} times, but the validation accepts one more.");
        }
    }

    private static void CheckResearch(List<string> violations, PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands,
        Hexagon hexagon, PossibleHexagonOrders orders)
    {
        // A second research on the hexagon would replace the requested one instead of adding to it.
        if (commands.TryGetValue(hexagon, out Command? command) && command.Research is not null)
        {
            if (!orders.Research.IsEmpty)
            {
                violations.Add($"{view.PlayerId}: a research is offered on {hexagon}, which already has one requested.");
            }
            return;
        }
        foreach (Upgrade upgrade in Enum.GetValues<Upgrade>())
        {
            bool offered = orders.Research.Contains(upgrade);
            bool accepted = IsValid(view, new CommandsBuilder(commands).Research(hexagon, upgrade).Commands);
            if (offered != accepted)
            {
                violations.Add($"{view.PlayerId}: researching {upgrade} on {hexagon} is {(offered ? "offered" : "not offered")}, but the validation {(accepted ? "accepts" : "refuses")} it.");
            }
        }
    }

    private static bool IsValid(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        return !CommandValidator.Validate(view, commands).Any();
    }
}
