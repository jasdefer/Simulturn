using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;

namespace Simulturn.Core.Engine;

/// <summary>
/// The occupancy rules: what a pending order keeps busy until it completes. A function gets the
/// values of one hexagon and says what is free; a negative value means the rule is broken.
/// <see cref="StateValidator"/> asks with the state, <see cref="CommandValidator"/> with the state
/// plus the commands, a client with what it wants to order next. None of them compares on its own.
/// </summary>
public static class Occupancy
{
    /// <summary>
    /// The number of units of every type the buildings train at the same time: one unit per
    /// building of the type that trains it (<see cref="GameSettings.TrainingBuilding"/>).
    /// </summary>
    public static Army TrainingSlots(Compound compound)
    {
        return Army.PerUnit(unit => compound[GameSettings.TrainingBuilding(unit)]);
    }

    /// <summary>
    /// The number of units of every type that can still be put into training. Every unit in
    /// training keeps its own building busy until it completes.
    /// </summary>
    /// <param name="compound">The buildings of the player on the hexagon.</param>
    /// <param name="trainings">The trainings on the hexagon, whatever their completion turn.</param>
    public static Army FreeTrainingSlots(Compound compound, Army trainings)
    {
        return TrainingSlots(compound) - trainings;
    }

    /// <summary>
    /// The number of dots no construction keeps busy. Every building under construction keeps
    /// its own dot on the hexagon until it completes. A free dot can start a construction; whether
    /// it may leave also depends on a research on the hexagon (<see cref="HoldsResearch"/>).
    /// </summary>
    /// <param name="army">The army of the player on the hexagon.</param>
    /// <param name="constructions">The constructions on the hexagon, whatever their completion turn.</param>
    public static int FreeDots(Army army, Compound constructions)
    {
        return army.Dot - constructions.Sum();
    }

    /// <summary>
    /// Whether the army keeps a research on its hexagon alive: a research needs a dot there
    /// from the turn it is ordered until it completes.
    /// </summary>
    /// <param name="army">The army of the player on the hexagon.</param>
    public static bool HoldsResearch(Army army)
    {
        return army.Dot > 0;
    }

    /// <summary>
    /// The dots the movements of the commands bring to every hexagon. A dot that arrives in a turn
    /// keeps a research alive (<see cref="HoldsResearch"/>), so the last dot may leave when another comes.
    /// </summary>
    internal static Dictionary<Hexagon, int> ArrivingDots(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        Dictionary<Hexagon, int> arrivingDots = [];
        foreach (MovementOrder movement in commands.Values.SelectMany(command => command.Movements))
        {
            arrivingDots[movement.Destination] = arrivingDots.GetValueOrDefault(movement.Destination) + movement.Army.Dot;
        }
        return arrivingDots;
    }

    /// <summary>
    /// The space the buildings give a player for units. Units take it from the moment they are
    /// ordered (<see cref="Costs.Training(GameSettings, Army)"/>).
    /// </summary>
    public static int ProvidedSpace(GameSettings settings, Compound compound)
    {
        return compound * settings.ProvidedSpace;
    }
}
