using SimulturnDomain.Settings;

namespace SimulturnDomain.Entities;
public record Game(ushort Turn,
    IReadOnlyCollection<Player> Players,
    GameSettings Settings,
    IReadOnlyDictionary<Coordinates, ushort> MatterPerCoordinates);