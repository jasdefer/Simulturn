using Simulturn.Core.Model;

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
        Army slots = Army.Empty;
        foreach (Unit unit in EnumValues.Units)
        {
            slots = slots.AddUnit(unit, compound[GameSettings.TrainingBuilding(unit)]);
        }
        return slots;
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
}
