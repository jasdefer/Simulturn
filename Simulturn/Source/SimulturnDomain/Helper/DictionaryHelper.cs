using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;
using System.Numerics;

namespace SimulturnDomain.Helper;
public static class DictionaryHelper
{
    public static void AddOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
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

    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (dict.TryGetValue(key, out TValue? value))
        {
            return value;
        }
        return new TValue();
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

    public static HexMap<T> ToHexMap<T>(this IDictionary<Coordinates, T> dict)
    {
        return new HexMap<T>(dict);
    }

    public static TurnMap<T> ToTurnMap<T>(this IDictionary<ushort, T> dict)
    {
        return new TurnMap<T>(dict);
    }

    public static TurnMap<HexMap<T>> ToTurnHexMap<T>(this IDictionary<ushort, IDictionary<Coordinates, T>> dict)
    {
        return new TurnMap<HexMap<T>>(dict.ToDictionary(x => x.Key, x => new HexMap<T>(x.Value)));
    }

    public static PlayerMap<HexMap<T>> ToTurnPlayerHexMap<T>(this IDictionary<string, IDictionary<Coordinates, T>> dict)
    {
        return new PlayerMap<HexMap<T>>(dict.ToDictionary(x => x.Key, x => new HexMap<T>(x.Value)));
    }

    public static HexMap<PlayerMap<T>> ToHexPlayerMap<T>(this IDictionary<Coordinates, IDictionary<string, T>> dict)
    {
        return new HexMap<PlayerMap<T>>(dict.ToDictionary(x => x.Key, x => new PlayerMap<T>(x.Value)));
    }

    public static HexMap<PlayerMap<T>> ToHexPlayerMap<T>(this IDictionary<Coordinates, Dictionary<string, T>> dict)
    {
        return new HexMap<PlayerMap<T>>(dict.ToDictionary(x => x.Key, x => new PlayerMap<T>(x.Value)));
    }

    public static PlayerMap<HexMap<T>> ToPlayerHexMap<T>(this IDictionary<string, IDictionary<Coordinates, T>> dict)
    {
        return new PlayerMap<HexMap<T>>(dict.ToDictionary(x => x.Key, x => new HexMap<T>(x.Value)));
    }
}