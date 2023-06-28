using SimulturnDomain.Settings;
using System.Collections.Immutable;

namespace SimulturnDomain.Entities;
public class Game
{
    public Game(string id, IEnumerable<Player> players, GameSettings gameSettings)
    {
        Id = id;
        Players = players.ToImmutableArray();
        GameSettings = gameSettings;
    }

    public string Id { get; }
    public ushort Turn { get; set; } = 0;
    public ImmutableArray<Player> Players { get; }
    public GameSettings GameSettings { get; }
}