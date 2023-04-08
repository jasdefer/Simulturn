using SimulturnDomain.Entities;

namespace SimulturnApplication.Queries.GetPlayerData;
public record PlayerDataViewModel(ushort Mass,
    ushort UsedSpace,
    ushort AvailableSpace,
    IReadOnlyDictionary<Coordinates, HexagonDataDto> HexagonsPerCoordinates,
    ChangesDto Changes);
