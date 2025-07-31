using Simulturn.Core.Model;

namespace Simulturn.Core.Extensions;
public static class DictionaryExtensions
{
    public static Dictionary<TKey1, Dictionary<TKey2, TValue>> Copy<TKey1, TKey2, TValue>(this IDictionary<TKey1, ImmutableDictionary<TKey2, TValue>> nestedDict)
        where TKey1 : notnull where TKey2 : notnull
    {
        return nestedDict
            .ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));

    }

    public static ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>> ToImmutable<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> nestedDict)
        where TKey1 : notnull where TKey2 : notnull
    {
        return nestedDict.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableDictionary(y => y.Key, y => y.Value));
    }

    public static void Merge<TKey>(this Dictionary<TKey, Army> dict, TKey key, Army army)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out Army existingArmy))
        {
            dict.Add(key, army);
        }
        Army newArmy = existingArmy + army;
        if (newArmy.IsEmpty)
        {
            dict.Remove(key);
            return;
        }
        dict[key] = newArmy;
        return;
    }

    public static void Merge<TKey>(this Dictionary<TKey, Compound> dict, TKey key, Compound compound)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out Compound existingCompound))
        {
            dict.Add(key, compound);
        }
        Compound newArmy = existingCompound + compound;
        if (newArmy.IsEmpty)
        {
            dict.Remove(key);
            return;
        }
        dict[key] = newArmy;
        return;
    }

    public static void MergeArmies(this PlayerHexagonArmies armies, PlayerHexagonArmies delta)
    {
        foreach (var playerId in delta.Keys)
        {
            if(!armies.TryGetValue(playerId, out var playerArmies))
            {
                playerArmies =  [];
                armies.Add(playerId, playerArmies);
            }
            foreach((Hexagon hexagon, Army armyDelta) in delta[playerId])
            {
                if(playerArmies.TryGetValue(hexagon, out Army existingArmy))
                {
                    var newArmy = existingArmy + armyDelta;
                    if(newArmy.IsEmpty)
                    {
                        playerArmies.Remove(hexagon);
                    }
                    else
                    {
                        playerArmies[hexagon] = newArmy;
                    }
                }
                else
                {
                    playerArmies.Add(hexagon, armyDelta);
                }
            }
        }
    }

    public static void MergeCompounds(this PlayerHexagonCompound compounds, PlayerHexagonCompound delta)
    {
        foreach (var playerId in delta.Keys)
        {
            if (!compounds.TryGetValue(playerId, out var playerCompounds))
            {
                playerCompounds = [];
                compounds.Add(playerId, playerCompounds);
            }
            foreach ((Hexagon hexagon, Compound compoundDelta) in delta[playerId])
            {
                if (playerCompounds.TryGetValue(hexagon, out Compound existingCompound))
                {
                    var newCompound = existingCompound + compoundDelta;
                    if (newCompound.IsEmpty)
                    {
                        playerCompounds.Remove(hexagon);
                    }
                    else
                    {
                        playerCompounds[hexagon] = newCompound;
                    }
                }
                else
                {
                    playerCompounds.Add(hexagon, compoundDelta);
                }
            }
        }
    }
}
