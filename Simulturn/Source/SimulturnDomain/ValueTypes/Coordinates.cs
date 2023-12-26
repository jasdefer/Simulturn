namespace SimulturnDomain.ValueTypes;
public readonly struct Coordinates : IEquatable<Coordinates>
{
    public Coordinates(short x, short y)
    {
        X = x;
        Y = y;
    }

    public short X { get; init; }
    public short Y { get; init; }
    public short Z => Convert.ToInt16(-X - Y);

    public override bool Equals(object? obj)
    {
        return obj is Coordinates coordinates && Equals(coordinates);
    }

    public bool Equals(Coordinates other)
    {
        return X == other.X &&
               Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Coordinates left, Coordinates right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Coordinates left, Coordinates right)
    {
        return !(left == right);
    }
}