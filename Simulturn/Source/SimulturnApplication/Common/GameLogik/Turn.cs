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
                var fight = Fight(armiesPerPlayer, game.Settings.FightExponent);
                game.FightsPerCoordinatesPerTurn[game.Turn][coordinates] = fight;
            }
        }
    }

    public static Fight Fight(IReadOnlyDictionary<string,Army> armiesPerPlayer, double fightExponent)
    {
        var fighters = armiesPerPlayer.Keys.ToArray();
        var lossesPerPlayer = fighters.ToDictionary(x => x, x => new Army());
        for (int i = 0; i < fighters.Length - 1; i++)
        {
            var army1 = armiesPerPlayer[fighters[i]];
            for (int j = i+1; j < fighters.Length; j++)
            {
                var army2 = armiesPerPlayer[fighters[j]];
                (Army losses1, Army losses2) = Fight(army1, army2, fightExponent);
                losses1 = losses1 + lossesPerPlayer[fighters[i]];
                lossesPerPlayer[fighters[i]] = losses1.Max(army1);
                losses2 = losses2 + lossesPerPlayer[fighters[j]];
                lossesPerPlayer[fighters[j]] = losses2.Max(army1);
            }
        }
        var fight = new Fight(armiesPerPlayer, lossesPerPlayer);
        return fight;
    }

    public static (Army losses1, Army losses2) Fight(Army army1, Army army2, double fightExponent)
    {
        var strength1 = army1.GetStrengthOver(army2, fightExponent);
        var strength2 = army2.GetStrengthOver(army1, fightExponent);
        if(strength1 > strength2)
        {
            return (army1.MultiplyAndRoundUp(strength2 / (double)strength1), army2);
        }
        else
        {
            return (army1, army2.MultiplyAndRoundUp(strength1 / (double)strength2));
        }
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
