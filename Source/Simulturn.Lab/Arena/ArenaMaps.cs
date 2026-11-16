using Simulturn.Core.Model;
using Simulturn.Core.Setup;

namespace Simulturn.Lab.Arena;

/// <summary>
/// A map type by name. <paramref name="Create"/> builds the settings of a game for a seed;
/// the same seed gives the same map.
/// </summary>
public sealed record ArenaMap(string Name, Func<int, GameSettings> Create);

/// <summary>
/// The map types of the arena: the types of the map generator, for two players, with the standard rules.
/// </summary>
public static class ArenaMaps
{
    public static ImmutableArray<ArenaMap> All { get; } =
    [
        new ArenaMap("Open", seed => DefaultSettings.Create(MapGeneratorOptions.Open with { Seed = seed })),
        new ArenaMap("HomeOnly", seed => DefaultSettings.Create(MapGeneratorOptions.HomeOnly with { Seed = seed })),
        new ArenaMap("Expansions", seed => DefaultSettings.Create(MapGeneratorOptions.Expansions with { Seed = seed })),
        new ArenaMap("Guarded", seed => DefaultSettings.Create(MapGeneratorOptions.Expansions with
        {
            Seed = seed,
            CenterGuard = new Army() { Triangle = 2, Circle = 2, Square = 2 },
            ExpansionGuard = new Army() { Dot = 3 }
        }))
    ];
}
