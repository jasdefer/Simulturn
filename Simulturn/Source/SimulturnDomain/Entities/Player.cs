namespace SimulturnDomain.Entities;
public class Player
{
    private readonly Dictionary<TurnCoordinates, Army> _trainings;
    private readonly Dictionary<TurnCoordinates, Structure> _constructions;
    private readonly Dictionary<TurnDirection, Army> _movements;

    public string Name { get; }
    public bool HasEndedTurn { get; set; }

    public Player(string name)
    {
        Name = name;
        _trainings = new Dictionary<TurnCoordinates, Army>();
        _constructions = new Dictionary<TurnCoordinates, Structure>();
        _movements = new Dictionary<TurnDirection, Army>();
    }

    public void AddTraining(TurnCoordinates turnCoordinates, Army army)
    {
        _trainings.Add(turnCoordinates, army);
    }

    public void AddTraining(ushort turn, Coordinates coordinates, Army army)
    {
        AddTraining(new TurnCoordinates(turn, coordinates), army);
    }
}
