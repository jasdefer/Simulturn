using SimulturnDomain.DataStructures;
using SimulturnDomain.Settings;

namespace SimulturnDomain.Model;
public record Game(string Id,
    TurnDictionary TurnDictionary,
    GameSettings GameSettings);