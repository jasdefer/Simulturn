using System.Collections.Immutable;

namespace SimulturnDomain.Entities;
public class Player
{
    /// <summary>
    /// For each turn and coordinate store the number of completed trainings
    /// </summary>
    private readonly Dictionary<ushort, Dictionary<Coordinates, Army>> _trainings;
    /// <summary>
    /// For each turn and coordinate store the number of completed constructions
    /// </summary>
    private readonly Dictionary<ushort, Dictionary<Coordinates, Structure>> _constructions;
    /// <summary>
    /// For each turn, origin coordinates and destination coordinates store moving armies
    /// </summary>
    private readonly Dictionary<ushort, Dictionary<Coordinates, Dictionary<Coordinates, Army>>> _movements;

    public string Name { get; }
    public bool HasEndedTurn { get; set; }

    public Player(string name)
    {
        Name = name;
        _trainings = new Dictionary<ushort, Dictionary<Coordinates, Army>>();
        _constructions = new Dictionary<ushort, Dictionary<Coordinates, Structure>>();
        _movements = new Dictionary<ushort, Dictionary<Coordinates, Dictionary<Coordinates, Army>>>();
    }

    public void AddTraining(ushort turn, Coordinates coordinates, Army army)
    {
        if (!_trainings.ContainsKey(turn))
        {
            _trainings.Add(turn, new Dictionary<Coordinates, Army>());
        }
        if (!_trainings[turn].ContainsKey(coordinates))
        {
            _trainings[turn][coordinates] = army;
        }
        else
        {
            _trainings[turn][coordinates] += army;
        }
    }

    public ImmutableDictionary<Coordinates, Army> GetTrainings(ushort turn)
    {
        if (_trainings.ContainsKey(turn))
        { 
            return _trainings[turn].ToImmutableDictionary();
        }
        return ImmutableDictionary<Coordinates, Army>.Empty;
    }

    public void AddConstruction(ushort turn, Coordinates coordinates, Structure structure)
    {
        if (!_constructions.ContainsKey(turn))
        {
            _constructions.Add(turn, new Dictionary<Coordinates, Structure>());
        }
        if (!_constructions[turn].ContainsKey(coordinates))
        {
            _constructions[turn][coordinates] = structure;
        }
        else
        {
            _constructions[turn][coordinates] += structure;
        }
    }

    public ImmutableDictionary<Coordinates, Structure> GetConstructions(ushort turn)
    {
        if (_constructions.ContainsKey(turn))
        {
            return _constructions[turn].ToImmutableDictionary();
        }
        return ImmutableDictionary<Coordinates, Structure>.Empty;
    }

    public void AddMovement(ushort turn, Coordinates origin, Coordinates destination, Army army)
    {
        if (!_movements.ContainsKey(turn))
        {
            _movements.Add(turn, new Dictionary<Coordinates, Dictionary<Coordinates, Army>>());
        }
        if (!_movements[turn].ContainsKey(origin))
        {
            _movements[turn].Add(origin, new Dictionary<Coordinates, Army>());
        }
        if (!_movements[turn][origin].ContainsKey(destination))
        {
            _movements[turn][origin].Add(destination, army);
        }
        else
        {
            _movements[turn][origin][destination] += army;
        }
    }
}
