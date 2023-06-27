namespace SimulturnDomain.Entities;
public struct TurnDirection : IEquatable<TurnDirection>
{
    public TurnDirection(ushort turn, Coordinates origin, Coordinates destination)
    {
        Turn = turn;
        Origin = origin;
        Destination = destination;
    }

    public ushort Turn { get; set; }
    public Coordinates Origin { get; set; }
    public Coordinates Destination { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is TurnDirection direction && Equals(direction);
    }

    public bool Equals(TurnDirection other)
    {
        return Turn == other.Turn &&
               EqualityComparer<Coordinates>.Default.Equals(Origin, other.Origin) &&
               EqualityComparer<Coordinates>.Default.Equals(Destination, other.Destination);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Turn, Origin, Destination);
    }

    public static bool operator ==(TurnDirection left, TurnDirection right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TurnDirection left, TurnDirection right)
    {
        return !(left == right);
    }
}
