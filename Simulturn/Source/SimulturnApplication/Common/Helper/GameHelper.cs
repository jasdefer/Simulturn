using SimulturnDomain.Entities;
using System.Linq;

namespace SimulturnApplication.Common.Helper;
public static class GameHelper
{
    public static void GetState(Game game)
    {
        var state = new State(game.Players, game.GameSettings.Coordinates);
        foreach (var player in game.Players)
        {
            state.AddMatter(player, game.GameSettings.StartMatter);
        }
        for (ushort turn = 0; turn < game.Turn; turn++)
        {
            foreach (var player in game.Players)
            {
                var movements = player.GetMovements(turn);
                var trainings = player.GetTrainings(turn);
                var constructions = player.GetConstructions(turn);

            }
        }
    }
}
