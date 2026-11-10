using Simulturn.Core.Model;

namespace Simulturn.Core.Setup;

/// <summary>
/// Where the upgrades of a generated map can be researched.
/// </summary>
public enum ResearchLocation
{
    /// <summary>On the contested hexagon in the middle of the map.</summary>
    Center,

    /// <summary>On the start hexagon of every player.</summary>
    StartHexagons
}

/// <summary>
/// The parameters of <see cref="MapGenerator"/>. The same options always produce the same map.
/// The presets <see cref="Open"/>, <see cref="HomeOnly"/> and <see cref="Expansions"/> are the
/// map types; adjust them with a <c>with</c> expression.
/// </summary>
public sealed record MapGeneratorOptions
{
    /// <summary>
    /// Matter on every hexagon, upgrades researchable in the center.
    /// Expanding is always possible and never contested for long.
    /// </summary>
    public static MapGeneratorOptions Open { get; } = new();

    /// <summary>
    /// All matter sits on the start hexagons, expanding is impossible.
    /// Upgrades are researchable at home. A pure production and timing fight.
    /// </summary>
    public static MapGeneratorOptions HomeOnly { get; } = new()
    {
        Radius = 2,
        HexagonMatter = 0,
        StartHexagonMatter = 12000,
        ResearchLocation = ResearchLocation.StartHexagons
    };

    /// <summary>
    /// Small starting reserves, one expansion close to home per player and a rich contested
    /// center where the upgrades are researchable. Expanding and fighting for the middle
    /// is mandatory.
    /// </summary>
    public static MapGeneratorOptions Expansions { get; } = new()
    {
        HexagonMatter = 0,
        StartHexagonMatter = 2000,
        ExpansionsPerPlayer = 1,
        ExpansionMatter = 4000,
        CenterMatter = 6000
    };

    /// <summary>
    /// The players in seat order. Only 2, 3 and 6 players can be placed exactly symmetric on a hexagon grid.
    /// </summary>
    public ImmutableArray<string> PlayerIds { get; init; } = Maps.PlayerIds(2);

    /// <summary>
    /// The map is a disc of all hexagons within this distance to the center.
    /// </summary>
    public int Radius { get; init; } = 3;

    /// <summary>
    /// The distance of the start hexagons to the center. Without a value the players start on the edge.
    /// </summary>
    public int? StartRing { get; init; }

    /// <summary>
    /// Decides where the expansions lie. Maps without expansions do not depend on the seed.
    /// </summary>
    public int Seed { get; init; } = 1;

    /// <summary>
    /// The matter on every hexagon that is neither a start hexagon nor an expansion nor a center with its own matter.
    /// </summary>
    public int HexagonMatter { get; init; } = 1500;

    public int StartHexagonMatter { get; init; } = 1500;

    /// <summary>
    /// The matter in the center. Without a value the center is a hexagon like any other.
    /// </summary>
    public int? CenterMatter { get; init; }

    /// <summary>
    /// The number of hexagons with <see cref="ExpansionMatter"/> every player gets on their side of the map.
    /// </summary>
    public int ExpansionsPerPlayer { get; init; }

    public int ExpansionMatter { get; init; } = 4000;

    /// <summary>
    /// How far an expansion may be away from the start hexagon of its player.
    /// </summary>
    public int MaxExpansionDistance { get; init; } = 2;

    public ImmutableHashSet<Upgrade> ResearchableUpgrades { get; init; } = [Upgrade.DotUpgrade];

    public ResearchLocation ResearchLocation { get; init; } = ResearchLocation.Center;

    public Army StartArmy { get; init; } = Maps.StartArmy;

    public Compound StartCompound { get; init; } = Maps.StartCompound;
}
