using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using System.Numerics;

namespace Simulturn.Core.Extensions;

internal static class DictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dict)
        where TKey : notnull
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        public void Merge(TKey key, TValue addition)
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
        }
    }

    extension<TKey, TValue>(ImmutableDictionary<TKey, TValue>.Builder builder)
        where TKey : notnull
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        public void Merge(IReadOnlyDictionary<TKey, TValue> other)
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
    }

    extension<TInner>(IReadOnlyDictionary<string, TInner> dict)
        where TInner : IReadOnlyDictionary<Hexagon, Command>
    {
        public IReadOnlyDictionary<Hexagon, Command> GetOrDefault(string key)
        {
            if (dict.TryGetValue(key, out var innerDict))
            {
                return innerDict;
            }
            return ImmutableDictionary<Hexagon, Command>.Empty;
        }
    }

    extension<TKey1, TKey2, TValue>(ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder dict)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        public void MergeOrOverwrite(ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder other)
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
    }

    extension<TKey1, TKey2, TValue>(ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder dict)
        where TKey1 : notnull
        where TKey2 : notnull
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        public void Merge(ImmutableDictionary<TKey1, ImmutableDictionary<TKey2, TValue>.Builder>.Builder other)
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
    }

    extension(IEnumerable<(string PlayerId, Hexagon Origin, Hexagon Destination, Army Army)> movements)
    {
        public Dictionary<string, Dictionary<Hexagon, Command>> MovementsToDictionary()
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
    }

    extension(IEnumerable<(string PlayerId, Hexagon Hexagon, Command Command)> commands)
    {
        public Dictionary<string, Dictionary<Hexagon, Command>> CommandsToDictionary()
        {
            return commands.GroupBy(x => x.PlayerId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        x => x.Hexagon,
                        x => x.Command));
        }
    }
}
