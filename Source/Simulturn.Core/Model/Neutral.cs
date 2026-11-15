namespace Simulturn.Core.Model;

/// <summary>
/// The neutral armies: guards that belong to no player. They stand where the map puts them
/// (<see cref="HexagonSettings.NeutralArmy"/>), never move, have no upgrades and fight whoever
/// enters their hexagon.
/// </summary>
public static class Neutral
{
    /// <summary>
    /// The key of the neutral army wherever armies are keyed by player id: among the combatants
    /// of a fight, in a fight report and in the unit counts a player sees. No player may use it.
    /// </summary>
    public const string Id = "Neutral";
}
