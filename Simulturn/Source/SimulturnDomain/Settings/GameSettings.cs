using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record GameSettings(double FightExponent,
    ArmySettings ArmySettings,
    StructureSettings StructureSettings,
    IReadOnlyList<UpkeepLevel> UpkeepLevels,
    IReadOnlyDictionary<Coordinates, HexagonSettings> HexagonSettingsPerCoordinates);
