using SimulturnDomain.Entities;
using System.Collections.Immutable;

namespace SimulturnDomain.Settings;
public record GameSettings(double FightExponent,
    ArmySettings ArmySettings,
    StructureSettings StructureSettings,
    ImmutableList<UpkeepLevel> UpkeepLevels,
    ImmutableDictionary<Coordinates, HexagonSettings> HexagonSettingsPerCoordinates);
