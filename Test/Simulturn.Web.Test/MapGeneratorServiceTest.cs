using Simulturn.Core.Model;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class MapGeneratorServiceTest
{
    private static readonly string[] _twoPlayers = ["Player 1", "Player 2"];

    [Test]
    public void Generate_CreatesFullDisk()
    {
        var map = MapGeneratorService.Generate(new MapGeneratorOptions { Radius = 3 }, _twoPlayers);
        map.Count.ShouldBe(3 * 3 * 3 + 3 * 3 + 1); // 3r² + 3r + 1 = 37
    }

    [Test]
    public void Generate_PlacesEveryPlayerOnce()
    {
        string[] players = ["A", "B", "C"];
        var map = MapGeneratorService.Generate(new MapGeneratorOptions(), players);
        var starts = map.Values
            .Where(settings => settings.PlayerInitialization is not null)
            .Select(settings => settings.PlayerInitialization!.Value.StartingPlayerId)
            .ToList();
        starts.Order().ShouldBe(players.Order().ToList());
    }

    [Test]
    public void Generate_IsDeterministicPerSeed()
    {
        var options = new MapGeneratorOptions { Seed = 42 };
        var first = MapGeneratorService.Generate(options, _twoPlayers);
        var second = MapGeneratorService.Generate(new MapGeneratorOptions { Seed = 42 }, _twoPlayers);
        second.Count.ShouldBe(first.Count);
        foreach (var (hexagon, settings) in first)
        {
            // HexagonSettings is a record, but ImmutableArray equality is reference-based —
            // compare content field by field.
            var other = second[hexagon];
            other.Matter.ShouldBe(settings.Matter);
            other.IsBuildable.ShouldBe(settings.IsBuildable);
            other.MaxNumberOfUnitsGeneratingMatter.ShouldBe(settings.MaxNumberOfUnitsGeneratingMatter);
            other.PlayerInitialization.ShouldBe(settings.PlayerInitialization);
            other.ResearchableUpgrades.SequenceEqual(settings.ResearchableUpgrades).ShouldBeTrue();
        }
    }

    [Test]
    public void Generate_DifferentSeeds_ProduceDifferentPilePlacement()
    {
        var first = MapGeneratorService.Generate(new MapGeneratorOptions { Seed = 1, MatterPilesPerPlayer = 3 }, _twoPlayers);
        var second = MapGeneratorService.Generate(new MapGeneratorOptions { Seed = 99, MatterPilesPerPlayer = 3 }, _twoPlayers);
        PileHexagons(first).ShouldNotBe(PileHexagons(second));
    }

    [Test]
    public void Generate_TwoPlayers_MapIsPointSymmetric()
    {
        var options = new MapGeneratorOptions { Seed = 7, MatterPilesPerPlayer = 2 };
        var map = MapGeneratorService.Generate(options, _twoPlayers);

        var start1 = StartHexagon(map, "Player 1");
        var start2 = StartHexagon(map, "Player 2");

        // 180° rotation = point reflection: (x, y) → (−x, −y).
        new Hexagon((short)-start1.X, (short)-start1.Y).ShouldBe(start2);

        // Every matter pile must have a mirrored twin.
        var piles = PileHexagons(map);
        foreach (var pile in piles)
        {
            piles.ShouldContain(new Hexagon((short)-pile.X, (short)-pile.Y));
        }
    }

    [Test]
    public void Generate_StartHexes_HaveWorkersHeadquartersAndMatter()
    {
        var options = new MapGeneratorOptions { StartDots = 5, StartHexMatter = 10000 };
        var map = MapGeneratorService.Generate(options, _twoPlayers);
        foreach (var settings in map.Values.Where(settings => settings.PlayerInitialization is not null))
        {
            settings.PlayerInitialization!.Value.InitialArmy.Dot.ShouldBe((short)5);
            settings.PlayerInitialization!.Value.InitialCompound.Plane.ShouldBe((short)1);
            settings.Matter.ShouldBe(10000);
            settings.MaxNumberOfUnitsGeneratingMatter.Dot.ShouldBeGreaterThan((short)0);
        }
    }

    [Test]
    public void Generate_CenterHex_HasResearchableUpgrades()
    {
        var map = MapGeneratorService.Generate(new MapGeneratorOptions { DotUpgradeLevelsAtCenter = 2 }, _twoPlayers);
        map[new Hexagon(0, 0)].ResearchableUpgrades.Length.ShouldBe(2);
    }

    [Test]
    public void Generate_ProducesValidInitialGameState()
    {
        var settings = DefaultSettings.Standard(_twoPlayers, seed: 123);
        var state = new Simulturn.Core.Model.State.GameState(settings);
        state.IsValid().ShouldBeEmpty();
        state.PlayerIds.Count.ShouldBe(2);
    }

    private static Hexagon StartHexagon(IReadOnlyDictionary<Hexagon, HexagonSettings> map, string playerId) =>
        map.Single(pair => pair.Value.PlayerInitialization?.StartingPlayerId == playerId).Key;

    private static List<Hexagon> PileHexagons(IReadOnlyDictionary<Hexagon, HexagonSettings> map) =>
        map.Where(pair => pair.Value.Matter > 0 && pair.Value.PlayerInitialization is null)
            .Select(pair => pair.Key)
            .OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X)
            .ToList();
}
