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

    public static readonly Hexagon Origin = new(0, 0);

    /// <summary>
    /// The six steps to the neighbors of a hexagon. Each direction is the previous one rotated by 60°.
    /// </summary>
    public static readonly ImmutableArray<Hexagon> Directions =
    [
        new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
    ];

    public static Hexagon operator +(Hexagon left, Hexagon right) =>
        new((short)(left.X + right.X), (short)(left.Y + right.Y));

    /// <summary>
    /// The six adjacent hexagons, whether or not they exist on the map.
    /// </summary>
    public IEnumerable<Hexagon> Neighbors()
    {
        foreach (Hexagon direction in Directions)
        {
            yield return this + direction;
        }
    }

    /// <summary>
    /// Rotates the hexagon around <see cref="Origin"/> by <paramref name="sixths"/> times 60°.
    /// Six steps are a full turn, negative values rotate the other way.
    /// </summary>
    public Hexagon Rotate(int sixths)
    {
        Hexagon rotated = this;
        int steps = ((sixths % 6) + 6) % 6;
        for (int step = 0; step < steps; step++)
        {
            rotated = new Hexagon((short)(rotated.X + rotated.Y), (short)-rotated.X);
        }
        return rotated;
    }

    /// <summary>
    /// All hexagons with exactly the given distance to <see cref="Origin"/>, walked around the ring.
    /// Radius 0 is the origin itself, radius r has 6 * r hexagons.
    /// </summary>
    public static IEnumerable<Hexagon> Ring(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        if (radius == 0)
        {
            yield return Origin;
            yield break;
        }
        Hexagon hexagon = new((short)-radius, (short)radius);
        foreach (Hexagon direction in Directions)
        {
            for (int step = 0; step < radius; step++)
            {
                yield return hexagon;
                hexagon += direction;
            }
        }
    }

    /// <summary>
    /// All hexagons within the given distance to <see cref="Origin"/>.
    /// Radius 1 has 7 hexagons, radius 2 has 19, radius 3 has 37.
    /// </summary>
    public static IEnumerable<Hexagon> Disc(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        for (short x = (short)-radius; x <= radius; x++)
        {
            for (short y = (short)-radius; y <= radius; y++)
            {
                if (Math.Abs(x + y) <= radius)
                {
                    yield return new Hexagon(x, y);
                }
            }
        }
    }
}