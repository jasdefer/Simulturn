using Simulturn.Core.Model;

namespace Simulturn.Core.Setup;

/// <summary>
/// Generates a fair map from a few parameters. The map is a disc that looks the same from every
/// seat: the start hexagons and the expansions of all players are rotations of each other around
/// the center, so every player has the same matter at the same distances.
/// </summary>
public static class MapGenerator
{
    public static ImmutableDictionary<Hexagon, HexagonSettings> Generate(MapGeneratorOptions options)
    {
        int playerCount = options.PlayerIds.IsDefault ? 0 : options.PlayerIds.Length;
        if (playerCount is not (2 or 3 or 6))
        {
            throw new ArgumentOutOfRangeException(nameof(options),
                $"A symmetric map needs 2, 3 or 6 players, not {playerCount}.");
        }
        if (options.PlayerIds.Distinct().Count() != playerCount)
        {
            throw new ArgumentException("The player ids must be unique.", nameof(options));
        }
        if (options.Radius < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), $"The radius must be at least 1, not {options.Radius}.");
        }
        int startRing = options.StartRing ?? options.Radius;
        if (startRing < 1 || startRing > options.Radius)
        {
            throw new ArgumentOutOfRangeException(nameof(options),
                $"The start ring must lie between 1 and the radius {options.Radius}, not at {startRing}.");
        }

        // The rotation that maps the seat of one player onto the seat of the next one.
        int sixthsPerSeat = 6 / playerCount;
        Hexagon firstStart = new((short)-startRing, 0);
        Hexagon[] starts = [.. Enumerable.Range(0, playerCount).Select(seat => firstStart.Rotate(seat * sixthsPerSeat))];

        var map = Maps.Disc(options.Radius, options.HexagonMatter).ToBuilder();

        if (options.CenterMatter is int centerMatter)
        {
            map[Hexagon.Origin] = Maps.Hexagon(centerMatter);
        }
        foreach (Hexagon expansion in PickExpansions(options, map.Keys, starts))
        {
            for (int seat = 0; seat < playerCount; seat++)
            {
                map[expansion.Rotate(seat * sixthsPerSeat)] = Maps.Hexagon(options.ExpansionMatter);
            }
        }
        for (int seat = 0; seat < playerCount; seat++)
        {
            map[starts[seat]] = Maps.Start(options.PlayerIds[seat], options.StartHexagonMatter, options.StartArmy, options.StartCompound);
        }

        IEnumerable<Hexagon> researchHexagons = options.ResearchLocation switch
        {
            ResearchLocation.Center => [Hexagon.Origin],
            ResearchLocation.StartHexagons => starts,
            _ => throw new ArgumentOutOfRangeException(nameof(options), $"Unknown research location {options.ResearchLocation}.")
        };
        foreach (Hexagon hexagon in researchHexagons)
        {
            map[hexagon] = map[hexagon] with { ResearchableUpgrades = options.ResearchableUpgrades };
        }
        return map.ToImmutable();
    }

    /// <summary>
    /// Picks the expansions of the first player. The candidates are strictly closer to the first
    /// start hexagon than to any other, so the rotated copies for the other players never collide.
    /// </summary>
    private static List<Hexagon> PickExpansions(MapGeneratorOptions options, IEnumerable<Hexagon> hexagons, Hexagon[] starts)
    {
        if (options.ExpansionsPerPlayer < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options),
                $"The number of expansions per player must not be negative, but is {options.ExpansionsPerPlayer}.");
        }
        Hexagon ownStart = starts[0];
        List<Hexagon> candidates = [.. hexagons
            .Where(x => x != ownStart && x != Hexagon.Origin)
            .Where(x => x.DistanceTo(ownStart) <= options.MaxExpansionDistance)
            .Where(x => starts.Skip(1).All(other => x.DistanceTo(ownStart) < x.DistanceTo(other)))
            .OrderBy(x => x.X).ThenBy(x => x.Y)];
        if (candidates.Count < options.ExpansionsPerPlayer)
        {
            throw new ArgumentOutOfRangeException(nameof(options),
                $"The map has room for {candidates.Count} expansions per player, not for {options.ExpansionsPerPlayer}.");
        }

        Random random = new(options.Seed);
        List<Hexagon> expansions = [];
        for (int expansion = 0; expansion < options.ExpansionsPerPlayer; expansion++)
        {
            int index = random.Next(candidates.Count);
            expansions.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return expansions;
    }
}
