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
            player.EndedCurrentTurn = false;
        }
        Fight(game);
        game.Turn++;
    }

    public static void Fight(Game game)
    {
        foreach (var coordinates in game.Settings.Coordinates)
        {
            var fighters = game.Players.Where(x => x.Armies[coordinates].Any()).ToArray();
            if(fighters.Length > 0)
            {
                var armiesPerPlayer = fighters.ToDictionary(x => x.Name, x=> x.Armies[coordinates]);
                Fight(armiesPerPlayer, game.Settings);
            }
        }
    }

    public static void Fight(IReadOnlyDictionary<string,Army> armiesPerPlayer, GameSettings settings)
    {
        var fighters = armiesPerPlayer.Keys.ToArray();
        var lossesPerPlayer = new Dictionary<string, Army>();
        for (int i = 0; i < fighters.Length - 1; i++)
        {
            for (int j = i+1; j < fighters.Length; j++)
            {
                var strength1 = armiesPerPlayer[fighters[i]].GetStrengthOver(armiesPerPlayer[fighters[j]])
            }
        }
        var fight = new Fight(fighters, lossesPerPlayer);
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
