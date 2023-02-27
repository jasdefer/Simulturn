using SimulturnCore.Model.Settings;

namespace SimulturnCore.Model;
public class Game
{
    public Game(Player[] players, GameSettings settings)
    {
        Players = players ?? throw new ArgumentNullException(nameof(players));
        Settings = settings;
        Turn = 0;
    }

    public Player[] Players { get; }
    public ushort Turn { get; set; }
    public GameSettings Settings { get; } = new GameSettings();
}
