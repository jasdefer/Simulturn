using SimulturnCore.Model;
using SimulturnCore.Model.Change;
using SimulturnCore.Model.Settings;

namespace SimulturnCore.Helper;
public static class Turn
{
    public static void End(Game game)
    {
        GenerateChanges(game.Players, game.Turn, game.Settings);
        Fight(game.Players, game.Turn, game.Settings);
        ApplyChanges(game.Players, game.Turn);
        game.Turn++;
    }

    private static void ApplyChanges(Player[] players, ushort turn)
    {
        foreach (var player in players)
        {
            ApplyChanges(player, turn);
        }
    }

    private static void Fight(Player[] players, ushort turn, GameSettings settings)
    {
        foreach (var coordinates in settings.Coordinates)
        {
            var fightingPlayers = players
                .Where(x => x.Armies.ContainsKey(coordinates))
                .ToArray();
            if (players.Length > 1)
            {
                Fight(coordinates, fightingPlayers, settings, turn);
            }
        }
    }

    private static void GenerateChanges(Player[] players, ushort turn, GameSettings settings)
    {
        foreach (var player in players)
        {
            player.ChangesPerTurn[turn] = GenerateChanges(player.OrdersPerTurn[turn], settings);
        }
    }

    public static void Fight(Coordinates coordinates, Player[] players, GameSettings settings, ushort turn)
    {
        throw new NotImplementedException();
    }

    public static void ApplyChanges(Player player, ushort turn)
    {
        throw new NotImplementedException();
    }

    public static ChangeSummary GenerateChanges(Order order, GameSettings settings)
    {
        throw new NotImplementedException();
    }
}