namespace SimulturnDomain.DataStructures;
public class TurnMap<T>
{
    private readonly Dictionary<ushort, T> _map;
    private static readonly Dictionary<ushort, T> _empty = new Dictionary<ushort, T>();

    public TurnMap(IDictionary<ushort, T> initialMap)
    {
        _map = new Dictionary<ushort, T>(initialMap);
    }

    public T this[ushort turn] => _map.TryGetValue(turn, out var value) ? value : throw new KeyNotFoundException("The given turn are not present in the map.");

    public IEnumerable<ushort> Keys => _map.Keys;
    public IEnumerable<T> Values => _map.Values;

    public bool ContainsKey(ushort turn) => _map.ContainsKey(turn);

    public static TurnMap<T> Empty()
    {
        return new TurnMap<T>(_empty);
    }
}