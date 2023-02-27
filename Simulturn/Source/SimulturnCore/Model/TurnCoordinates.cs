namespace SimulturnCore.Model;
public class TurnCoordinates<T>
{
    private readonly Dictionary<ushort, Dictionary<Coordinates, T>> changes = new();
    public T this[ushort turn, Coordinates coordinates]
    {
        get
        {
            return changes[turn][coordinates];
        }
        set
        {
            if (!changes.ContainsKey(turn))
            {
                changes.Add(turn, new Dictionary<Coordinates, T>());
            }

            if (!changes[turn].ContainsKey(coordinates))
            {
                changes[turn].Add(coordinates, value);
            }
            else
            {
                changes[turn][coordinates] = value;
            }
        }
    }
}