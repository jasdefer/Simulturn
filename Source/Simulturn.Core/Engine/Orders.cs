using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

/// <summary>
/// What a player can order. A client builds a turn step by step: it asks <see cref="Possible"/>
/// what the commands entered so far leave, takes one order, adds it to its <see cref="CommandsBuilder"/>
/// and asks again. It computes no rule itself and never tries a command the validation refuses.
/// The limits come from the rules
/// <see cref="CommandValidator"/> checks: <see cref="Occupancy"/>, <see cref="Costs"/> and the
/// research rules of the validator itself.
/// </summary>
public static class Orders
{
    private static readonly Command _noCommand = new();

    /// <summary>
    /// What the player of the view can still order after the commands entered so far. The commands
    /// must be valid; every order taken from the result keeps them valid. A defeated player can
    /// order nothing.
    /// </summary>
    public static PossibleOrders Possible(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        GameSettings settings = view.GameSettings;
        PlayerState player = view.PlayerState;
        OrderCost budget = Costs.Budget(settings, player, commands);
        var hexagons = ImmutableDictionary.CreateBuilder<Hexagon, PossibleHexagonOrders>();
        if (Standing.IsDefeated(player))
        {
            return new PossibleOrders(budget, hexagons.ToImmutable());
        }

        Dictionary<Hexagon, int> arrivingDots = Occupancy.ArrivingDots(commands);
        foreach (Hexagon hexagon in player.Armies.Keys.Union(player.Compounds.Keys))
        {
            Command command = commands.GetValueOrDefault(hexagon) ?? _noCommand;
            Army army = player.Armies.GetValueOrDefault(hexagon);
            Army remainingArmy = army - command.Movements.Select(x => x.Army).Sum();
            Compound constructions = player.Constructions.On(hexagon) + command.Construction;
            bool researches = command.Research is not null || player.Researches.ContainsKey(hexagon);

            PossibleHexagonOrders possible = new(
                PossibleTraining(settings, player, hexagon, command, budget),
                PossibleConstruction(settings, hexagon, remainingArmy, constructions, budget),
                PossibleResearch(view, commands, hexagon, command, army),
                Movable(remainingArmy, constructions, researches, arrivingDots.GetValueOrDefault(hexagon)));
            if (!possible.IsEmpty)
            {
                hexagons.Add(hexagon, possible);
            }
        }
        return new PossibleOrders(budget, hexagons.ToImmutable());
    }

    /// <summary>
    /// A unit needs a free building of its type, its matter and its space.
    /// </summary>
    private static Army PossibleTraining(GameSettings settings, PlayerState player, Hexagon hexagon, Command command, OrderCost budget)
    {
        Compound compound = player.Compounds.GetValueOrDefault(hexagon);
        Army freeSlots = Occupancy.FreeTrainingSlots(compound, player.Trainings.On(hexagon) + command.Training);
        return Army.PerUnit(unit =>
            Math.Clamp(Affordable(budget, Costs.Training(settings, unit), takesSpace: true), 0, Math.Max(0, (int)freeSlots[unit])));
    }

    /// <summary>
    /// A building needs a buildable hexagon, a free dot that stays and its matter.
    /// </summary>
    private static Compound PossibleConstruction(GameSettings settings, Hexagon hexagon, Army remainingArmy, Compound constructions, OrderCost budget)
    {
        if (!settings.HexagonSettings.TryGetValue(hexagon, out HexagonSettings? hexagonSettings) || !hexagonSettings.IsBuildable)
        {
            return Compound.Empty;
        }
        int freeDots = Math.Max(0, Occupancy.FreeDots(remainingArmy, constructions));
        return Compound.PerBuilding(building =>
            Math.Clamp(Affordable(budget, Costs.Construction(settings, building), takesSpace: false), 0, freeDots));
    }

    /// <summary>
    /// The research rules are a yes or no with many conditions, so every upgrade the hexagon
    /// offers is put to the validator. A hexagon takes one research per turn.
    /// </summary>
    private static ImmutableArray<Upgrade> PossibleResearch(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands, Hexagon hexagon, Command command, Army army)
    {
        if (command.Research is not null ||
            !Occupancy.HoldsResearch(army) ||
            !view.GameSettings.HexagonSettings.TryGetValue(hexagon, out HexagonSettings? hexagonSettings))
        {
            return [];
        }
        var possible = ImmutableArray.CreateBuilder<Upgrade>();
        foreach (Upgrade upgrade in hexagonSettings.ResearchableUpgrades.Order())
        {
            if (CommandValidator.AcceptsResearch(view, new CommandsBuilder(commands).Research(hexagon, upgrade).Commands, hexagon))
            {
                possible.Add(upgrade);
            }
        }
        return possible.ToImmutable();
    }

    /// <summary>
    /// Every unit not yet moving can leave, except the dots that build and the last dot of a
    /// research. That last dot can leave once another dot is on its way to the hexagon.
    /// </summary>
    private static Army Movable(Army remainingArmy, Compound constructions, bool researches, int arrivingDots)
    {
        int movableDots = Math.Min(remainingArmy.Dot, Occupancy.FreeDots(remainingArmy, constructions));
        while (movableDots > 0 && researches &&
            !Occupancy.HoldsResearch(remainingArmy with { Dot = (short)(remainingArmy.Dot - movableDots + arrivingDots) }))
        {
            movableDots--;
        }
        return Army.PerUnit(unit => Math.Max(0, unit == Unit.Dot ? movableDots : remainingArmy[unit]));
    }

    /// <summary>
    /// How many orders of the price the budget covers. The space only limits an order that
    /// trains: with more units than space nothing is trained, whatever the unit takes.
    /// </summary>
    private static int Affordable(OrderCost budget, OrderCost price, bool takesSpace)
    {
        if (budget.Matter < 0 || (takesSpace && budget.Space < 0))
        {
            return 0;
        }
        int count = int.MaxValue;
        if (price.Matter > 0)
        {
            count = Math.Min(count, budget.Matter / price.Matter);
        }
        if (takesSpace && price.Space > 0)
        {
            count = Math.Min(count, budget.Space / price.Space);
        }
        return count;
    }
}
