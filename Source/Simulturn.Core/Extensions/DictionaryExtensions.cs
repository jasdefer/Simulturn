using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using System.Numerics;

namespace Simulturn.Core.Extensions;
public static class DictionaryExtensions
{
    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue addition)
        where TKey : notnull
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        if (!dict.TryGetValue(key, out TValue? existingValue))
        {
            dict.Add(key, addition);
            return;
        }
        TValue newValue = existingValue + addition;
        if (newValue.Equals(default(TValue)))
        {
            dict.Remove(key);
            return;
        }
        dict[key] = newValue;
        return;
    }

    public static void Merge<TKey, TValue>(this ImmutableDictionary<TKey, TValue>.Builder builder, IReadOnlyDictionary<TKey, TValue> other)
        where TValue : IAdditionOperators<TValue, TValue, TValue>
        where TKey : notnull
    {
        foreach (var kvp in other)
        {
            if (builder.TryGetValue(kvp.Key, out var existingValue))
            {
                builder[kvp.Key] += kvp.Value;
            }
            else
            {
                builder.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public static IReadOnlyDictionary<Hexagon, Command> GetOrDefault<TInner>(this IReadOnlyDictionary<string, TInner> dict, string key)
        where TInner : IReadOnlyDictionary<Hexagon, Command>
    {
        if (dict.TryGetValue(key, out var innerDict))
        {
            return innerDict;
        }
        return ImmutableDictionary<Hexagon, Command>.Empty;
    }

    public static void MergeOrOverwrite<TKey1, TKey2, TValue>(this ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder dict,
        ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder other)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        foreach (var kvp in other)
        {
            if (!dict.TryGetValue(kvp.Key, out var innerDict))
            {
                dict[kvp.Key] = kvp.Value;
                continue;
            }
            foreach (var innerKvp in kvp.Value)
            {
                innerDict[innerKvp.Key] = innerKvp.Value;
            }
        }
    }

    public static void Merge<TKey1, TKey2, TValue>(this ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder dict,
        ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder other)
        where TKey1 : notnull
        where TKey2 : notnull
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        foreach (var kvp in other)
        {
            if (!dict.TryGetValue(kvp.Key, out var innerDict))
            {
                dict[kvp.Key] = kvp.Value;
                continue;
            }
            foreach (var innerKvp in kvp.Value)
            {
                if (innerDict.TryGetValue(innerKvp.Key, out var existingValue))
                {
                    innerDict[innerKvp.Key] += innerKvp.Value;
                }
                else
                {
                    innerDict.Add(innerKvp.Key, innerKvp.Value);
                }
            }
        }
    }

    public static Dictionary<string, Dictionary<Hexagon, Command>> MovementsToDictionary(this IEnumerable<(string PlayerId, Hexagon Origin, Hexagon Destination, Army Army)> movements)
    {
        return movements.GroupBy(x => x.PlayerId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    x => x.Origin,
                    x => new Command
                    {
                        MovementCommands = [new MovementCommand()
                        {
                            Destination = x.Destination,
                            Army = x.Army
                        }]
                    }));
    }

    public static Dictionary<string, Dictionary<Hexagon, Command>> CommandsToDictionary(this IEnumerable<(string PlayerId, Hexagon Hexagon, Command Command)> commands)
    {
        return commands.GroupBy(x => x.PlayerId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    x => x.Hexagon,
                    x => x.Command));
    }
}
