namespace SimulturnDomain.DataStructures;
public class PlayerMap<T>
{
    private readonly Dictionary<string, T> _map;

    public PlayerMap(IDictionary<string, T> initialMap)
    {
        _map = new Dictionary<string, T>(initialMap);
    }

    public T this[string player] => _map.TryGetValue(player, out var value) ? value : throw new KeyNotFoundException("The given player is not present in the map.");

    public bool ContainsKey(string player) => _map.ContainsKey(player);
    public IEnumerable<string> Keys => _map.Keys;
    public IEnumerable<T> Values => _map.Values;
}