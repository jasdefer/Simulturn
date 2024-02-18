using SimulturnDomain.ValueTypes;
using System.Collections;

namespace SimulturnDomain.DataStructures;
public class HexMap<T> : IEnumerable<KeyValuePair<Coordinates, T>>
{
    private readonly Dictionary<Coordinates, T> _map;
    private static readonly Dictionary<Coordinates, T> _empty = [];

    public HexMap(IDictionary<Coordinates, T> initialMap)
    {
        _map = new Dictionary<Coordinates, T>(initialMap);
    }

    public T this[Coordinates coordinates] => _map.TryGetValue(coordinates, out var value) ? value : throw new KeyNotFoundException("The given coordinates are not present in the map.");

    public IEnumerable<Coordinates> Keys => _map.Keys;
    public IEnumerable<T> Values => _map.Values;

    public Dictionary<Coordinates, T> ToDictionary()
    {
        return new Dictionary<Coordinates, T>(_map);
    }

    public bool ContainsKey(Coordinates coordinates) => _map.ContainsKey(coordinates);

    public static HexMap<T> Empty()
    {
        return new HexMap<T>(_empty);
    }

    public HexMap<U> ToHexMap<U>(Func<T, U> conversion)
    {
        return new HexMap<U>(_map.ToDictionary(x => x.Key, x => conversion(x.Value)));
    }

    public IEnumerator<KeyValuePair<Coordinates, T>> GetEnumerator()
    {
        return _map.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _map.GetEnumerator();
    }
}