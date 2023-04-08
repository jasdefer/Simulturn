using SimulturnDomain.Entities;

namespace SimulturnApplication.Queries.GetPlayerData;
public record ChangesDto(ushort Income,
    ushort Upkeep,
    ushort NetIncome,
    IReadOnlyDictionary<Coordinates, FightDto> FightPerCoordinates,
    IReadOnlyDictionary<Coordinates, Structure> BuiltStructuresPerCoordinates,
    IReadOnlyDictionary<Coordinates, Army> TraindAmriesPerCoordinates);

