namespace Simulturn.Core.Model;

public readonly struct Hexagon : IEquatable<Hexagon>
{
    public short X { get; }
    public short Y { get; }
    public short Z => (short)(-X - Y);

    public Hexagon(short x, short y)
    {
        X = x;
        Y = y;
    }

    // Compare based on X and Y only, since Z is deterministic
    public bool Equals(Hexagon other) =>
        X == other.X && Y == other.Y;

    public override bool Equals(object? obj) =>
        obj is Hexagon other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(X, Y);

    public static bool operator ==(Hexagon left, Hexagon right) =>
        left.Equals(right);

    public static bool operator !=(Hexagon left, Hexagon right) =>
        !left.Equals(right);

    public override string ToString() =>
        $"Hex({X}, {Y}, {Z})";

    public int DistanceTo(Hexagon other)
    {
        return (Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z)) / 2;
    }
}