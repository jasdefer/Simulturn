using SimulturnDomain.Entities;
using System.Collections.Immutable;

namespace SimulturnDomain.Settings;
public record GameSettings(
    double FightExponent,
    double StartMatter,
    ArmySettings ArmySettings,
    StructureSettings StructureSettings,
    ImmutableArray<UpkeepLevel> UpkeepLevels,
    ImmutableDictionary<Coordinates, HexagonSettings> HexagonSettingsPerCoordinates,
    int? Seed = null)
{
    public static GameSettings Default(int? seed = null)
    {
        var upkeepLevels = new UpkeepLevel[]
        {
            new UpkeepLevel(50, 0.2),
            new UpkeepLevel(80, 0.5)
        }.ToImmutableArray();
        var hexagonSettings = new Dictionary<Coordinates, HexagonSettings>()
        {
        }.ToImmutableDictionary();
        return new GameSettings(2,
            500,
            ArmySettings.Default(),
            StructureSettings.Default(),
            upkeepLevels,
            hexagonSettings,
            seed);
    }
};
