using SimulturnDomain.DataStructures;
using SimulturnDomain.Settings;
using System.Collections.Immutable;

namespace SimulturnDomain.Model;
public record Game(string Id,
    TurnMap<ImmutableDictionary<string, List<Order>>> TurnsPerPlayer,
    GameSettings GameSettings);