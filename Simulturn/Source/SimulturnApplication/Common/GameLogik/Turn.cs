using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.GameLogik;
public static class Turn
{
    public static void EndTurn(Game game)
    {

        game.Turn++;
    }
}
