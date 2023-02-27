using SimulturnCore.Model;

namespace SimulturnCore.Helper;
public static class Turn
{
    public static void End(Game game)
    {
        foreach (var player in game.Players)
        {
            //GenerateIncome(player, game.Settings);
            //ResolveArmyAndStructureChanges(player);
            //ResolveArmyQueue(player, game.Settings);
            //ResolveStructureQueue(player, game.Settings);
            //MoveArmy(player);
            //Fight(player);
            //Resolve fight changes
        }
        game.Turn++;
    }
}