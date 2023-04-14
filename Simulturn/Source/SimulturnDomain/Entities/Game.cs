using SimulturnDomain.Settings;

namespace SimulturnDomain.Entities;
public class Game
{
    public Game(IReadOnlyCollection<Player> players, GameSettings settings)
    {
        Players = players ?? throw new ArgumentNullException(nameof(players));
        FightsPerCoordinatesPerTurn = new Dictionary<ushort, Dictionary<Coordinates, Fight>>();
        Settings = settings;
        Turn = 0;
    }

    public IReadOnlyCollection<Player> Players { get; }
    public Dictionary<ushort, Dictionary<Coordinates, Fight>> FightsPerCoordinatesPerTurn { get; }
    public ushort Turn { get; set; }
    public GameSettings Settings { get; } = new GameSettings();
}
