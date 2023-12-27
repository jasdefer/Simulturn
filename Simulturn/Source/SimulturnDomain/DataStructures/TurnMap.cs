namespace SimulturnDomain.DataStructures;
public class TurnMap<T>
{
    private readonly Dictionary<ushort, T> _map;

    public TurnMap(Dictionary<ushort, T> initialMap)
    {
        _map = new Dictionary<ushort, T>(initialMap);
    }

    public T this[ushort turn] => _map.TryGetValue(turn, out var value) ? value : throw new KeyNotFoundException("The given turn are not present in the map.");

    public IEnumerable<ushort> Keys => _map.Keys;
    public IEnumerable<T> Values => _map.Values;
}
