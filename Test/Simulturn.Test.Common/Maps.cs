using Simulturn.Core.Model;

namespace Simulturn.Test.Common;

/// <summary>
/// Builds the hexagon settings of a test map. Start with <see cref="Disc"/> or <see cref="Line"/>
/// and place players with <c>SetItem(hexagon, Maps.Start(...))</c>.
/// </summary>
public static class Maps
{
    public static readonly Army StartArmy = new() { Dot = 5 };
    public static readonly Compound StartCompound = new() { Plane = 1 };
    public static readonly Army MaxHarvestingUnits = new() { Dot = 12 };

    /// <summary>
    /// A plain hexagon. Without matter it yields no income, no matter how many dots sit on it.
    /// </summary>
    public static HexagonSettings Hexagon(int matter = 0)
    {
        return new HexagonSettings()
        {
            Matter = matter,
            MaxNumberOfUnitsGeneratingMatter = MaxHarvestingUnits
        };
    }

    /// <summary>
    /// The start hexagon of a player: 5 dots and a plane unless specified otherwise.
    /// </summary>
    public static HexagonSettings Start(string playerId, int matter = 1000, Army? army = null, Compound? compound = null)
    {
        return Hexagon(matter) with
        {
            PlayerInitialization = (playerId, army ?? StartArmy, compound ?? StartCompound)
        };
    }

    public static HexagonSettings WithResearch(this HexagonSettings settings, params Upgrade[] upgrades)
    {
        return settings with { ResearchableUpgrades = [.. upgrades] };
    }

    /// <summary>
    /// A hexagon disc around (0, 0). Radius 1 has 7 hexagons, radius 2 has 19, radius 3 has 37.
    /// </summary>
    public static ImmutableDictionary<Hexagon, HexagonSettings> Disc(int radius, int matter = 0)
    {
        var builder = ImmutableDictionary.CreateBuilder<Hexagon, HexagonSettings>();
        for (short x = (short)-radius; x <= radius; x++)
        {
            for (short y = (short)-radius; y <= radius; y++)
            {
                if (Math.Abs(x + y) > radius)
                {
                    continue;
                }
                builder[new Hexagon(x, y)] = Hexagon(matter);
            }
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// A straight line of hexagons on y = 0 from x = <paramref name="fromX"/> to x = <paramref name="toX"/>, both inclusive.
    /// </summary>
    public static ImmutableDictionary<Hexagon, HexagonSettings> Line(int fromX, int toX, int matter = 0)
    {
        var builder = ImmutableDictionary.CreateBuilder<Hexagon, HexagonSettings>();
        for (int x = fromX; x <= toX; x++)
        {
            builder[new Hexagon((short)x, 0)] = Hexagon(matter);
        }
        return builder.ToImmutable();
    }
}
