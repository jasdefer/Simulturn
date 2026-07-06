using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.AI.Analysis;

/// <summary>
/// Unit and building roles derived from the game settings, so that AI players
/// adapt to the settings instead of hardcoding units.
/// </summary>
public record SettingsAnalysis
{
    /// <summary>
    /// The unit that generates income and constructs buildings.
    /// </summary>
    public required Unit Worker { get; init; }

    /// <summary>
    /// The cheapest unit, used to scout the map.
    /// </summary>
    public required Unit Scout { get; init; }

    /// <summary>
    /// The fighting units ordered by cost ascending.
    /// </summary>
    public required ImmutableArray<Unit> Fighters { get; init; }

    /// <summary>
    /// The building that enables resource gathering and trains workers.
    /// </summary>
    public required Building IncomeBuilding { get; init; }

    /// <summary>
    /// The cheapest building per provided space.
    /// </summary>
    public required Building SpaceBuilding { get; init; }
}
