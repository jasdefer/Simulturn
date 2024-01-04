using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.DataStructures;
public class HexMap<T>
{
    private readonly Dictionary<Coordinates, T> _map;

    public HexMap(IDictionary<Coordinates, T> initialMap)
    {
        _map = new Dictionary<Coordinates, T>(initialMap);
    }

    public T this[Coordinates coordinates] => _map.TryGetValue(coordinates, out var value) ? value : throw new KeyNotFoundException("The given coordinates are not present in the map.");

    public IEnumerable<Coordinates> Keys => _map.Keys;
    public IEnumerable<T> Values => _map.Values;
}