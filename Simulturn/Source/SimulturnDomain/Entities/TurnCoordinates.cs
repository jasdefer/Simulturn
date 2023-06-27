namespace SimulturnDomain.Entities;
public struct TurnCoordinates : IEquatable<TurnCoordinates>
{
    public TurnCoordinates(ushort turn, Coordinates coordinates)
    {
        Coordinates = coordinates;
        Turn = turn;
    }

    public Coordinates Coordinates { get; set; }
    public ushort Turn { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is TurnCoordinates coordinates && Equals(coordinates);
    }

    public bool Equals(TurnCoordinates other)
    {
        return EqualityComparer<Coordinates>.Default.Equals(Coordinates, other.Coordinates) &&
               Turn == other.Turn;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coordinates, Turn);
    }

    public static bool operator ==(TurnCoordinates left, TurnCoordinates right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TurnCoordinates left, TurnCoordinates right)
    {
        return !(left == right);
    }
}
