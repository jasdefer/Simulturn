using SimulturnDomain.Enums;

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

    public Coordinates GetNeighbor(HexDirection direction)
    {
        return direction switch
        {
            HexDirection.NorthEast => new Coordinates(X, Convert.ToInt16(Y - 1)),
            HexDirection.East => new Coordinates(Convert.ToInt16(X + 1), Convert.ToInt16(Y - 1)),
            HexDirection.SouthEast => new Coordinates(Convert.ToInt16(X + 1), Y),
            HexDirection.SouthWest => new Coordinates(X, Convert.ToInt16(Y + 1)),
            HexDirection.West => new Coordinates(Convert.ToInt16(X - 1), Convert.ToInt16(Y + 1)),
            HexDirection.NorthWest => new Coordinates(Convert.ToInt16(X - 1), Y),
            _ => throw new ArgumentException("Invalid hex direction")
        };
    }
}