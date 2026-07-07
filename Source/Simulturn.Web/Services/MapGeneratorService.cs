using Simulturn.Core.Model;
using Simulturn.Web.Models;
using System.Collections.Immutable;

namespace Simulturn.Web.Services;

/// <summary>
/// Generates symmetric maps from parameters: a hex disk, player starts evenly spaced on a ring,
/// per-player matter piles replicated by rotation (exact for 60°-divisible player spacings,
/// nearest-hex rounded otherwise), and a contested research hex at the center.
/// Pure and deterministic per <see cref="MapGeneratorOptions.Seed"/>.
/// </summary>
public static class MapGeneratorService
{
    private static readonly (short X, short Y)[] _ringDirections =
    [
        (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1),
    ];

    public static ImmutableDictionary<Hexagon, HexagonSettings> Generate(
        MapGeneratorOptions options,
        IReadOnlyList<string> playerIds)
    {
        if (playerIds.Count == 0)
        {
            throw new ArgumentException("At least one player is required.", nameof(playerIds));
        }
        int radius = Math.Max(1, options.Radius);
        int startRing = Math.Clamp(options.StartRingRadius, 1, radius);

        var map = Disk(radius).ToDictionary(hexagon => hexagon, _ => HexagonSettings.Empty);

        // Player starts, evenly spaced on the start ring.
        var ring = Ring(startRing);
        var startHexagons = new Hexagon[playerIds.Count];
        for (int player = 0; player < playerIds.Count; player++)
        {
            var startHexagon = ring[player * ring.Count / playerIds.Count];
            startHexagons[player] = startHexagon;
            map[startHexagon] = new HexagonSettings
            {
                IsBuildable = true,
                Matter = options.StartHexMatter,
                MaxNumberOfUnitsGeneratingMatter = new Army { Dot = ClampShort(options.MaxHarvestersPerHex) },
                PlayerInitialization = (playerIds[player], new Army { Dot = ClampShort(options.StartDots) }, new Compound { Plane = 1 }),
            };
        }

        // Contested research hex at the center.
        if (options.DotUpgradeLevelsAtCenter > 0)
        {
            var center = new Hexagon(0, 0);
            map[center] = map[center] with
            {
                ResearchableUpgrades = [.. Enumerable.Repeat(Upgrade.DotUpgrade, options.DotUpgradeLevelsAtCenter)],
            };
        }

        // Symmetric matter piles: pick offsets near player 0's start, replicate to every player
        // by rotating around the center so each player gets an identical local economy.
        var random = new Random(options.Seed);
        var pileOffsets = PickPileOffsets(random, options, map.Keys.ToHashSet(), startHexagons[0]);
        for (int player = 0; player < playerIds.Count; player++)
        {
            double angle = 2 * Math.PI * player / playerIds.Count;
            foreach (var offset in pileOffsets)
            {
                var pile = RotateAroundCenter(offset, angle);
                if (!map.TryGetValue(pile, out var existing) ||
                    existing.PlayerInitialization is not null ||
                    !existing.ResearchableUpgrades.IsEmpty ||
                    existing.Matter > 0)
                {
                    continue;
                }
                map[pile] = existing with
                {
                    Matter = options.MatterPerPile,
                    MaxNumberOfUnitsGeneratingMatter = new Army { Dot = ClampShort(options.MaxHarvestersPerHex) },
                };
            }
        }

        return map.ToImmutableDictionary();
    }

    private static List<Hexagon> PickPileOffsets(
        Random random,
        MapGeneratorOptions options,
        HashSet<Hexagon> board,
        Hexagon start)
    {
        // Candidates: hexes near player 0's start (closer to it than to any rotated copy is
        // approximated by "within half the start-ring circumference"), excluding start and center.
        var candidates = board
            .Where(hexagon => hexagon != start &&
                              hexagon != new Hexagon(0, 0) &&
                              hexagon.DistanceTo(start) >= 1 &&
                              hexagon.DistanceTo(start) <= Math.Max(2, options.StartRingRadius))
            .OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X)
            .ToList();

        var offsets = new List<Hexagon>();
        for (int pile = 0; pile < options.MatterPilesPerPlayer && candidates.Count > 0; pile++)
        {
            int index = random.Next(candidates.Count);
            offsets.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return offsets;
    }

    /// <summary>
    /// Rotates a hexagon around the board center by an arbitrary angle, rounding to the
    /// nearest hexagon. Exact for multiples of 60°.
    /// </summary>
    private static Hexagon RotateAroundCenter(Hexagon hexagon, double angle)
    {
        // Work in cartesian space (any consistent hex→pixel mapping works for rotation).
        double sqrt3 = Math.Sqrt(3);
        double x = sqrt3 * (hexagon.X + hexagon.Z / 2.0);
        double y = 1.5 * hexagon.Z;

        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        double rotatedX = x * cos - y * sin;
        double rotatedY = x * sin + y * cos;

        // Back to fractional cube coordinates, then cube-round.
        double z = rotatedY / 1.5;
        double q = rotatedX / sqrt3 - z / 2.0;
        return CubeRound(q, -q - z, z);
    }

    private static Hexagon CubeRound(double x, double y, double z)
    {
        double roundedX = Math.Round(x);
        double roundedY = Math.Round(y);
        double roundedZ = Math.Round(z);

        double deltaX = Math.Abs(roundedX - x);
        double deltaY = Math.Abs(roundedY - y);
        double deltaZ = Math.Abs(roundedZ - z);

        if (deltaX > deltaY && deltaX > deltaZ)
        {
            roundedX = -roundedY - roundedZ;
        }
        else if (deltaY > deltaZ)
        {
            roundedY = -roundedX - roundedZ;
        }

        return new Hexagon((short)roundedX, (short)roundedY);
    }

    private static IEnumerable<Hexagon> Disk(int radius)
    {
        for (short x = (short)-radius; x <= radius; x++)
        {
            for (short y = (short)-radius; y <= radius; y++)
            {
                if (Math.Abs(x + y) <= radius)
                {
                    yield return new Hexagon(x, y);
                }
            }
        }
    }

    private static List<Hexagon> Ring(int radius)
    {
        // Standard cube-coordinate ring walk: start at direction[4] · radius, then walk
        // radius steps in each of the six directions.
        var ring = new List<Hexagon>(6 * radius);
        short x = (short)(_ringDirections[4].X * radius);
        short y = (short)(_ringDirections[4].Y * radius);
        for (int side = 0; side < 6; side++)
        {
            for (int step = 0; step < radius; step++)
            {
                ring.Add(new Hexagon(x, y));
                x += _ringDirections[side].X;
                y += _ringDirections[side].Y;
            }
        }
        return ring;
    }

    private static short ClampShort(int value) => (short)Math.Clamp(value, 0, short.MaxValue);
}
