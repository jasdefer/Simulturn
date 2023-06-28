using System.Numerics;

namespace SimulturnDomain.Helper;
public static class DictionaryHelper
{
    public static void AddOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        where TValue : IAdditionOperators<TValue, TValue, TValue>
        where TKey : notnull
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, value);
        }
        else
        {
            dict[key] += value;
        }
    }
}
