using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Settings;
public record GameSettings(
    double FightExponent,
    short StartMatter,
    ArmySettings ArmySettings,
    StructureSettings StructureSettings,
    ImmutableArray<UpkeepLevel> UpkeepLevels,
    HexMap<HexagonSettings> HexagonSettingsPerCoordinates,
    int Seed = 1)
{
    public IEnumerable<Coordinates> Coordinates => HexagonSettingsPerCoordinates.Keys;

    public static GameSettings Default(int seed = 1)
    {
        var upkeepLevels = new UpkeepLevel[]
        {
            new UpkeepLevel(50, 0.2),
            new UpkeepLevel(80, 0.5)
        }.ToImmutableArray();
        Army points = new Army(0, 0, 0, 0, 5);
        var hexagonSettings = new Dictionary<Coordinates, HexagonSettings>()
        {
            { new Coordinates(-2,+0), new HexagonSettings(1000, points, true) },
            { new Coordinates(-2,+1), new HexagonSettings(1000, points, true) },
            { new Coordinates(-2,+2), new HexagonSettings(1000, points, true) },
            { new Coordinates(-1,-1), new HexagonSettings(1000, points, true) },
            { new Coordinates(-1,+0), new HexagonSettings(1000, points, true) },
            { new Coordinates(-1,+1), new HexagonSettings(1000, points, true) },
            { new Coordinates(-1,+2), new HexagonSettings(1000, points, true) },
            { new Coordinates(+0,-2), new HexagonSettings(1000, points, true) },
            { new Coordinates(+0,-1), new HexagonSettings(1000, points, true) },
            { new Coordinates(+0,+0), new HexagonSettings(1000, points, true) },
            { new Coordinates(+0,+1), new HexagonSettings(1000, points, true) },
            { new Coordinates(+0,+2), new HexagonSettings(1000, points, true) },
            { new Coordinates(+1,-2), new HexagonSettings(1000, points, true) },
            { new Coordinates(+1,-1), new HexagonSettings(1000, points, true) },
            { new Coordinates(+1,+0), new HexagonSettings(1000, points, true) },
            { new Coordinates(+1,+1), new HexagonSettings(1000, points, true) },
            { new Coordinates(+2,-2), new HexagonSettings(1000, points, true) },
            { new Coordinates(+2,-1), new HexagonSettings(1000, points, true) },
            { new Coordinates(+2,+0), new HexagonSettings(1000, points, true) },
        };
        return new GameSettings(2,
            500,
            ArmySettings.Default(),
            StructureSettings.Default(),
            upkeepLevels,
            new HexMap<HexagonSettings>(hexagonSettings),
            seed);
    }
};