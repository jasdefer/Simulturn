namespace SimulturnDomain.Entities;
public class State
{
    private readonly Dictionary<Player, Dictionary<Coordinates, Army>> _armies;
    private readonly Dictionary<Player, Dictionary<Coordinates, Structure>> _structures;
    private readonly Dictionary<Player, short> _matter;

    public State(IEnumerable<Player> players, IEnumerable<Coordinates> coordinates)
    {
        _armies = players.ToDictionary(x => x, x => coordinates.ToDictionary(y => y, y => new Army()));
        _structures = players.ToDictionary(x => x, x => coordinates.ToDictionary(y => y, y => new Structure()));
        _matter = players.ToDictionary(x => x, x => (short)0);
    }

    public void AddMatter(Player player, short matter)
    {
        _matter[player] += matter;
    }

    public void AddArmy(Player player, Coordinates coordinates, Army army)
    {
        _armies[player].AddOrAdd(coordinates, army);
    }
}
