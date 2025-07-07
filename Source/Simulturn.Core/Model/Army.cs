namespace Simulturn.Core.Model;

public readonly struct Army : IEquatable<Army>
{
    public short Triangle { get; init; }
    public short Circle { get; init; }
    public short Square { get; init; }
    public short Dot { get; init; }

    public bool IsEmpty => Triangle == 0 && Circle == 0 && Square == 0 && Dot == 0;
    public int Total => Triangle + Circle + Square + Dot;

    public bool Equals(Army other) =>
        Triangle == other.Triangle &&
        Circle == other.Circle &&
        Square == other.Square &&
        Dot == other.Dot;

    public override bool Equals(object? obj) =>
        obj is Army other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Triangle, Circle, Square, Dot);

    public static bool operator ==(Army left, Army right) => left.Equals(right);
    public static bool operator !=(Army left, Army right) => !left.Equals(right);

    public static Army operator +(Army a, Army b) =>
        new()
        {
            Triangle = (short)(a.Triangle + b.Triangle),
            Circle = (short)(a.Circle + b.Circle),
            Square = (short)(a.Square + b.Square),
            Dot = (short)(a.Dot + b.Dot)
        };

    public static Army operator -(Army a, Army b) =>
        new()
        {
            Triangle = (short)(a.Triangle - b.Triangle),
            Circle = (short)(a.Circle - b.Circle),
            Square = (short)(a.Square - b.Square),
            Dot = (short)(a.Dot - b.Dot)
        };

    public override string ToString() =>
        $"Army(Triangles: {Triangle}, Circles: {Circle}, Squares: {Square}, Dots: {Dot})";
}