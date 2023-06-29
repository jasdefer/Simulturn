using SimulturnDomain.Helper;
using System.Collections.Immutable;

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

    private State(Dictionary<Player, Dictionary<Coordinates, Army>> armies,
        Dictionary<Player, Dictionary<Coordinates, Structure>> structures,
        Dictionary<Player, short> matter)
    {
        _armies = armies;
        _structures = structures;
        _matter = matter;
    }

    public void AddMatter(Player player, short matter)
    {
        _matter[player] += matter;
    }

    public void AddArmy(Player player, Coordinates coordinates, Army army)
    {
        _armies[player].AddOrAdd(coordinates, army);
    }

    public void AddArmy(Player player, IReadOnlyDictionary<Coordinates, Army> armies)
    {
        _armies[player].Add(armies);
    }

    public void SubtractArmy(Player player, Coordinates coordinates, Army army)
    {
        _armies[player].AddOrSubtract(coordinates, army);
    }

    public void SubtractArmy(Player player, IReadOnlyDictionary<Coordinates, Army> armies)
    {
        _armies[player].Subtract(armies);
    }

    public void AddStructure(Player player, Coordinates coordinates, Structure structure)
    {
        _structures[player].AddOrAdd(coordinates, structure);
    }

    public void AddStructure(Player player, IReadOnlyDictionary<Coordinates, Structure> structures)
    {
        _structures[player].Add(structures);
    }

    public static State Diff(State a, State b)
    {
        var armies = new Dictionary<Player, Dictionary<Coordinates, Army>>();
        var structures = new Dictionary<Player, Dictionary<Coordinates, Structure>>();
        var matter = new Dictionary<Player, short>();
        foreach (var player in a._armies.Keys)
        {
            armies.Add(player, a._armies[player].ToDictionary(x => x.Key, x => x.Value - b._armies[player][x.Key]));
            structures.Add(player, a._structures[player].ToDictionary(x => x.Key, x => x.Value - b._structures[player][x.Key]));
            matter[player] = (short)(a._matter[player] - b._matter[player]);
        }
        return new State(armies, structures, matter);
    }

    public static State Add(State a, State b)
    {
        var armies = new Dictionary<Player, Dictionary<Coordinates, Army>>();
        var structures = new Dictionary<Player, Dictionary<Coordinates, Structure>>();
        var matter = new Dictionary<Player, short>();
        foreach (var player in a._armies.Keys)
        {
            armies.Add(player, a._armies[player].ToDictionary(x => x.Key, x => x.Value + b._armies[player][x.Key]));
            structures.Add(player, a._structures[player].ToDictionary(x => x.Key, x => x.Value + b._structures[player][x.Key]));
            matter[player] = (short)(a._matter[player] + b._matter[player]);
        }
        return new State(armies, structures, matter);
    }
}