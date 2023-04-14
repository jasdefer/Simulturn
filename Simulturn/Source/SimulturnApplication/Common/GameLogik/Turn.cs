using SimulturnApplication.Common.Extensions;
using SimulturnDomain.Entities;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Common.GameLogik;
public static class Turn
{
    public static void EndTurn(Game game)
    {
        foreach (var player in game.Players)
        {
            AddIncome(game.Turn, player, game.Settings);
            ResolveChange(game.Turn, player);
        }
        game.Turn++;
    }

    public static void ResolveChange(ushort turn, Player player)
    {
        var changes = player.ChangesPerTurn[turn];
        player.Armies.Add(changes.TrainedArmiesPerCoordinates);
        player.Structures.Add(changes.BuiltStructuresPerCoordinates);
        player.Matter = changes.Income.Nett;
        foreach (var movement in changes.Movements)
        {
            player.Armies[movement.Origin] -= movement.Army;
            player.Armies[movement.Destination] += movement.Army;
        }
    }

    private static void AddIncome(ushort turn, Player player, GameSettings settings)
    {
        var income = GetIncome(player.Armies, player.Structures, settings);
        var nextTurn = (ushort)(turn + 1);
        if (!player.ChangesPerTurn.ContainsKey(nextTurn))
        {
            player.ChangesPerTurn.Add(turn, new Change());
        }
        player.ChangesPerTurn[nextTurn].Income = income;
    }

    public static void AddIncome(IReadOnlyDictionary<ushort, Change> changesPerTurn, int v)
    {
        throw new NotImplementedException();
    }

    public static Income GetIncome(IReadOnlyDictionary<Coordinates, Army> armies, IReadOnlyDictionary<Coordinates, Structure> structures, GameSettings settings)
    {
        throw new NotImplementedException();
    }
}
