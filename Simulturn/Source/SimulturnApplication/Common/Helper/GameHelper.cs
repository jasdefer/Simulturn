using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.Helper;
public static class GameHelper
{
    public static State GetState(Game game, ushort turn)
    {
        var state = new State(game.Players, game.GameSettings.Coordinates);
        foreach (var player in game.Players)
        {
            state.AddArmy(player, player.GetTrainings(turn));
            state.AddStructure(player, player.GetConstructions(turn));
            var movements = player.GetMovements(turn);
            foreach (var keyValuePair in movements)
            {
                var origin = keyValuePair.Key;
                var armyPerDestination = keyValuePair.Value;
                state.AddArmy(player, armyPerDestination);
                foreach (var armies in armyPerDestination.Values)
                {
                    state.SubtractArmy(player, origin, armies);
                }
            }
        }
        // ToDo Fights
        // ToDo Income
        return state;
    }
}