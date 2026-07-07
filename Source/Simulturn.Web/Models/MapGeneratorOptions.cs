namespace Simulturn.Web.Models;

/// <summary>Parameters for <see cref="Services.MapGeneratorService"/>. Deterministic per seed.</summary>
public sealed class MapGeneratorOptions
{
    /// <summary>Map radius: all hexagons within this cube distance of the center.</summary>
    public int Radius { get; set; } = 4;

    /// <summary>Ring on which player start hexes are placed, evenly spaced.</summary>
    public int StartRingRadius { get; set; } = 3;

    public int Seed { get; set; } = 1;

    /// <summary>Symmetric expansion matter piles per player.</summary>
    public int MatterPilesPerPlayer { get; set; } = 2;

    public int MatterPerPile { get; set; } = 6000;

    public int StartHexMatter { get; set; } = 10000;

    /// <summary>Max dots harvesting simultaneously on a matter hex.</summary>
    public int MaxHarvestersPerHex { get; set; } = 12;

    /// <summary>Researchable DotUpgrade levels on the contested center hex.</summary>
    public int DotUpgradeLevelsAtCenter { get; set; } = 2;

    public int StartDots { get; set; } = 5;
}
