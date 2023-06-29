using System.Numerics;

namespace SimulturnDomain.Helper;
public static class DictionaryHelper
{
    public static void AddOrAdd<TKey, TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, TValue value)
        where TValue : IAdditionOperators<TValue, TValue, TValue>
        where TKey : notnull
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
        }
        else
        {
            dictionary[key] += value;
        }
    }

    public static void AddOrSubtract<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>
        where TKey : notnull
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, new TValue());
        }
        dictionary[key] -= value;
    }

    public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        where TValue : IAdditionOperators<TValue, TValue, TValue>
        where TKey : notnull
    {
        foreach (var keyValuePair in b)
        {
            a.AddOrAdd(keyValuePair.Key, keyValuePair.Value);
        }
    }

    public static void Subtract<TKey, TValue>(this IDictionary<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue> 
        where TKey : notnull
    {
        foreach (var keyValuePair in b)
        {
            a.AddOrSubtract(keyValuePair.Key, keyValuePair.Value);
        }
    }
}