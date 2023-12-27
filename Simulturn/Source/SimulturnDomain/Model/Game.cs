using SimulturnDomain.Settings;

namespace SimulturnDomain.Model;
public record Game(string Id,
    IReadOnlySet<string> PlayerIds,
    GameSettings GameSettings);