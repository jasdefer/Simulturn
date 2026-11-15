using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

/// <summary>
/// The income rule. The turn resolution collects the income with it, a client forecasts with it
/// what the orders entered so far leave of the income.
/// </summary>
public static class Harvest
{
    /// <summary>
    /// The largest army that harvests on the hexagon at the same time. More units bring no more income.
    /// </summary>
    public static Army Capacity(GameSettings settings, Hexagon hexagon)
    {
        return settings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter;
    }

    /// <summary>
    /// What the army of a player harvests on one hexagon in one turn. It needs a plane of the
    /// player there. Every construction that starts this turn takes one dot off the harvest, the
    /// hexagon limits the number of harvesters (<see cref="Capacity"/>), and there is never more
    /// to get than the matter the hexagon has left.
    /// </summary>
    /// <param name="army">The army of the player on the hexagon at the start of the turn.</param>
    /// <param name="compound">The buildings of the player on the hexagon at the start of the turn.</param>
    /// <param name="startedConstructions">The constructions the player orders on the hexagon this turn.</param>
    /// <param name="remainingMatter">The matter the hexagon has left.</param>
    public static IncomeReport Of(GameSettings settings, Hexagon hexagon, Army army, Compound compound, Compound startedConstructions, int remainingMatter)
    {
        if (compound.Plane <= 0)
        {
            return new IncomeReport(Army.Empty, 0);
        }
        int busyDots = EnumValues.Buildings.Sum(building => Math.Max(0, (int)startedConstructions[building]));
        Army idleArmy = army with
        {
            Dot = (short)Math.Max(0, army.Dot - busyDots)
        };
        Army harvesters = Army.Min(idleArmy, Capacity(settings, hexagon));
        int matter = Math.Max(0, Math.Min(settings.Income * harvesters, remainingMatter));
        return new IncomeReport(harvesters, matter);
    }

    /// <summary>
    /// The income a player can expect from the turn with the given orders, per hexagon; hexagons
    /// without income are missing. The forecast knows only what the player sees: where another
    /// player harvests the same hexagon, the matter left may not be enough for both.
    /// </summary>
    public static ImmutableDictionary<Hexagon, IncomeReport> Forecast(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        var incomes = ImmutableDictionary.CreateBuilder<Hexagon, IncomeReport>();
        foreach ((Hexagon hexagon, Army army) in view.PlayerState.Armies)
        {
            // A hexagon with an own army on it is in sight, so its remaining matter is known.
            int remainingMatter = view.Observations[hexagon].RemainingMatter ?? 0;
            Compound startedConstructions = commands.TryGetValue(hexagon, out Command? command) ? command.Construction : Compound.Empty;
            IncomeReport income = Of(view.GameSettings, hexagon, army, view.PlayerState.Compounds.GetValueOrDefault(hexagon), startedConstructions, remainingMatter);
            if (income.Matter > 0)
            {
                incomes.Add(hexagon, income);
            }
        }
        return incomes.ToImmutable();
    }
}
