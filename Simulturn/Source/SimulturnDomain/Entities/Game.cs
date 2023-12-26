using SimulturnDomain.Settings;

namespace SimulturnDomain.Entities;
public record Game(string Id,
    IReadOnlySet<string> PlayerIds,
    GameSettings GameSettings);