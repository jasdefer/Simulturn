using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.Setup;

/// <summary>
/// The acceptance criteria of the work item map-generator-and-default-settings: the same options
/// produce the same map, every player has a start hexagon, the map looks the same from every seat,
/// and it is a valid game with the <see cref="DefaultSettings"/>.
/// </summary>
public sealed class MapGeneratorTest
{
    private static readonly int[] _playerCounts = [2, 3, 6];

    private static IEnumerable<(string Name, MapGeneratorOptions Options)> MapTypes()
    {
        yield return ("Open", MapGeneratorOptions.Open);
        yield return ("HomeOnly", MapGeneratorOptions.HomeOnly);
        yield return ("Expansions", MapGeneratorOptions.Expansions);
    }

    /// <summary>
    /// Everything a hexagon contributes to the game, with the seat instead of the player id.
    /// </summary>
    private static string Describe(HexagonSettings settings, MapGeneratorOptions options, int seatShift = 0)
    {
        string start = "-";
        if (settings.PlayerInitialization is (string playerId, Army army, Compound compound))
        {
            int seat = (options.PlayerIds.IndexOf(playerId) + seatShift) % options.PlayerIds.Length;
            start = $"seat {seat}, {army}, {compound}";
        }
        return $"matter {settings.Matter}, harvesters {settings.MaxNumberOfUnitsGeneratingMatter}, buildable {settings.IsBuildable}, " +
            $"research [{string.Join(", ", settings.ResearchableUpgrades.Order())}], neutral {settings.NeutralArmy}, start {start}";
    }

    private static string Describe(ImmutableDictionary<Hexagon, HexagonSettings> map, MapGeneratorOptions options)
    {
        return string.Join(Environment.NewLine, map
            .OrderBy(x => x.Key.X).ThenBy(x => x.Key.Y)
            .Select(x => $"{x.Key}: {Describe(x.Value, options)}"));
    }

    [Test]
    public void SameOptions_ProduceTheSameMap()
    {
        MapGeneratorOptions options = MapGeneratorOptions.Expansions with { Seed = 7, ExpansionsPerPlayer = 2 };

        var first = MapGenerator.Generate(options);
        var second = MapGenerator.Generate(options);

        Describe(second, options).ShouldBe(Describe(first, options));
    }

    [Test]
    public void DifferentSeeds_PlaceTheExpansionsDifferently()
    {
        var layouts = Enumerable.Range(1, 10)
            .Select(seed => MapGeneratorOptions.Expansions with { Seed = seed })
            .Select(options => Describe(MapGenerator.Generate(options), options))
            .Distinct();

        layouts.Count().ShouldBeGreaterThan(1);
    }

    [Test]
    public void MapsWithoutExpansions_DoNotDependOnTheSeed()
    {
        MapGeneratorOptions options = MapGeneratorOptions.Open;

        var first = MapGenerator.Generate(options with { Seed = 1 });
        var second = MapGenerator.Generate(options with { Seed = 2 });

        Describe(second, options).ShouldBe(Describe(first, options));
    }

    [Test]
    public void EveryPlayer_HasExactlyOneStartHexagonOnTheStartRing()
    {
        foreach (int playerCount in _playerCounts)
        {
            MapGeneratorOptions options = MapGeneratorOptions.Open with { PlayerIds = Maps.PlayerIds(playerCount), Radius = 4, StartRing = 3 };

            var map = MapGenerator.Generate(options);

            var starts = map.Where(x => x.Value.PlayerInitialization is not null).ToList();
            starts.Select(x => x.Value.PlayerInitialization!.Value.StartingPlayerId).ShouldBe(options.PlayerIds, ignoreOrder: true);
            starts.ShouldAllBe(x => x.Key.DistanceTo(Hexagon.Origin) == 3);
        }
    }

    [Test]
    public void WithoutStartRing_ThePlayersStartOnTheEdge()
    {
        var map = MapGenerator.Generate(MapGeneratorOptions.Open with { Radius = 4 });

        map[new Hexagon(-4, 0)].PlayerInitialization!.Value.StartingPlayerId.ShouldBe("Player01");
        map[new Hexagon(4, 0)].PlayerInitialization!.Value.StartingPlayerId.ShouldBe("Player02");
    }

    /// <summary>
    /// Fairness: rotating the map by one seat moves every hexagon onto an identical one,
    /// and the start hexagon of every player onto the start hexagon of the next player.
    /// </summary>
    [Test]
    public void Map_LooksTheSameFromEverySeat()
    {
        foreach ((string name, MapGeneratorOptions preset) in MapTypes())
        {
            foreach (int playerCount in _playerCounts)
            {
                for (int seed = 1; seed <= 5; seed++)
                {
                    MapGeneratorOptions options = preset with { PlayerIds = Maps.PlayerIds(playerCount), Seed = seed };

                    var map = MapGenerator.Generate(options);

                    foreach ((Hexagon hexagon, HexagonSettings settings) in map)
                    {
                        Hexagon rotated = hexagon.Rotate(6 / playerCount);
                        map.ShouldContainKey(rotated);
                        Describe(map[rotated], options).ShouldBe(Describe(settings, options, seatShift: 1),
                            $"Map {name}, {playerCount} players, seed {seed}, {hexagon} rotated to {rotated}.");
                    }
                }
            }
        }
    }

    [Test]
    public void EveryMapType_IsAValidGameWithTheDefaultSettings()
    {
        foreach ((string name, MapGeneratorOptions preset) in MapTypes())
        {
            foreach (int playerCount in _playerCounts)
            {
                MapGeneratorOptions options = preset with { PlayerIds = Maps.PlayerIds(playerCount) };

                GameState gameState = new(DefaultSettings.Create(options));

                gameState.IsValid().ShouldBeEmpty($"Map {name}, {playerCount} players.");
                gameState.PlayerIds.ShouldBe(options.PlayerIds, ignoreOrder: true);
            }
        }
    }

    [Test]
    public void Open_PutsMatterOnEveryHexagonAndResearchInTheCenter()
    {
        var map = MapGenerator.Generate(MapGeneratorOptions.Open);

        map.Count.ShouldBe(37);
        map.Values.ShouldAllBe(x => x.Matter == 1500);
        map.Where(x => !x.Value.ResearchableUpgrades.IsEmpty).Select(x => x.Key).ShouldBe([Hexagon.Origin]);
    }

    [Test]
    public void HomeOnly_PutsMatterAndResearchOnlyOnTheStartHexagons()
    {
        var map = MapGenerator.Generate(MapGeneratorOptions.HomeOnly);

        foreach ((Hexagon hexagon, HexagonSettings settings) in map)
        {
            bool isStart = settings.PlayerInitialization is not null;
            settings.Matter.ShouldBe(isStart ? 12000 : 0, $"{hexagon}");
            settings.ResearchableUpgrades.IsEmpty.ShouldBe(!isStart, $"{hexagon}");
        }
    }

    [Test]
    public void Expansions_GiveEveryPlayerTheirExpansionsCloseToHomeAndARichCenter()
    {
        MapGeneratorOptions options = MapGeneratorOptions.Expansions with { PlayerIds = Maps.PlayerIds(3), ExpansionsPerPlayer = 2 };

        var map = MapGenerator.Generate(options);

        var starts = map.Where(x => x.Value.PlayerInitialization is not null).Select(x => x.Key).ToList();
        var expansions = map.Where(x => x.Value.Matter == options.ExpansionMatter).Select(x => x.Key).ToList();
        expansions.Count.ShouldBe(6);
        foreach (Hexagon start in starts)
        {
            expansions.Count(x => x.DistanceTo(start) <= options.MaxExpansionDistance).ShouldBe(2, $"Start {start}.");
        }
        map[Hexagon.Origin].Matter.ShouldBe(6000);
        map[Hexagon.Origin].ResearchableUpgrades.ShouldBe([Upgrade.DotUpgrade]);
        starts.ShouldAllBe(x => map[x].Matter == 2000);
        map.Values.Sum(x => x.Matter).ShouldBe(3 * 2000 + 6 * 4000 + 6000);
    }

    [Test]
    public void StartArmyAndCompound_ComeFromTheOptions()
    {
        Army army = new() { Dot = 8, Triangle = 2 };
        Compound compound = new() { Plane = 1, Axis = 1 };

        var map = MapGenerator.Generate(MapGeneratorOptions.Open with { StartArmy = army, StartCompound = compound });

        map[new Hexagon(-3, 0)].PlayerInitialization.ShouldBe(("Player01", army, compound));
        map[new Hexagon(3, 0)].PlayerInitialization.ShouldBe(("Player02", army, compound));
    }

    [Test]
    public void PlayerCountWithoutExactSymmetry_Throws()
    {
        foreach (int playerCount in new[] { 0, 1, 4, 5, 7 })
        {
            MapGeneratorOptions options = MapGeneratorOptions.Open with { PlayerIds = Maps.PlayerIds(playerCount) };

            Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(options), $"{playerCount} players.");
        }
    }

    [Test]
    public void DuplicatePlayerIds_Throw()
    {
        MapGeneratorOptions options = MapGeneratorOptions.Open with { PlayerIds = ["Player01", "Player01"] };

        Should.Throw<ArgumentException>(() => MapGenerator.Generate(options));
    }

    [Test]
    public void RadiusOrStartRingOutsideTheMap_Throw()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(MapGeneratorOptions.Open with { Radius = 0 }));
        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(MapGeneratorOptions.Open with { StartRing = 0 }));
        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(MapGeneratorOptions.Open with { Radius = 3, StartRing = 4 }));
    }

    [Test]
    public void MoreExpansionsThanTheMapHasRoomFor_Throw()
    {
        // On a disc of radius 1 with six players every hexagon is a start hexagon or the center.
        MapGeneratorOptions options = MapGeneratorOptions.Expansions with { PlayerIds = Maps.PlayerIds(6), Radius = 1 };

        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(options));
        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.Generate(MapGeneratorOptions.Expansions with { ExpansionsPerPlayer = -1 }));
    }

    [Test]
    public void DefaultSettings_UseTheSeedOfTheMapOptions()
    {
        GameSettings settings = DefaultSettings.Create(MapGeneratorOptions.Expansions with { Seed = 42 });

        settings.Seed.ShouldBe(42);
        settings.StartMatter.ShouldBe(DefaultSettings.DefaultStartMatter);
    }

    [Test]
    public void PlayerIds_AreNumberedFromOne()
    {
        Maps.PlayerIds(3).ShouldBe(["Player01", "Player02", "Player03"]);
    }
}
