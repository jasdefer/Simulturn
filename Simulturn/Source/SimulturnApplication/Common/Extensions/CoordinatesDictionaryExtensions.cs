using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.Extensions;
public static class CoordinatesDictionaryExtensions
{
    public static void Incorporate<T>(this Dictionary<Coordinates, T> baseDict, Dictionary<Coordinates, T> dictToIncorporate, Func<T, T, T> add)
    {
        foreach (var item in dictToIncorporate)
        {
            if (!baseDict.ContainsKey(item.Key))
            {
                baseDict.Add(item.Key, item.Value);
            }
            else
            {
                baseDict[item.Key] = add(baseDict[item.Key], item.Value);
            }
        }
    }

    public static void Add(this Dictionary<Coordinates, Army> dict, Dictionary<Coordinates, Army> dictToAdd)
    {
        Incorporate(dict, dictToAdd, (Army a, Army b) => a + b);
    }

    public static void Add(this Dictionary<Coordinates, Structure> dict, Dictionary<Coordinates, Structure> dictToAdd)
    {
        Incorporate(dict, dictToAdd, (Structure a, Structure b) => a + b);
    }
}
