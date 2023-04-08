using SimulturnDomain.Entities;

namespace SimulturnApplication.Queries.GetPlayerData;
public abstract record HexagonDataDto();

public record OccupiedHexagon(Army Army,
    Structure Structure,
    ushort Matter) : HexagonDataDto;

public record NeutralHexagon(bool CreepsPresent,
    bool ContainsMatter) : HexagonDataDto;

public record NeutralHexagonWithDetails(Army Army,
    ushort Matter) : HexagonDataDto;

public record EnemyHexagonWithDetails(string PlayerId,
    Army Army,
    Structure Structure) : HexagonDataDto;

public record EnemyHexagon(string PlayerId,
    bool HasArmy,
    bool HasStructures);
