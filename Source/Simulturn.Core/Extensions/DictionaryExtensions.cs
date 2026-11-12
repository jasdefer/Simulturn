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
