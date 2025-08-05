using System.Numerics;

namespace Simulturn.Core.Model;

public readonly struct Army : IEquatable<Army>, IAdditionOperators<Army, Army, Army>
{
    public static readonly Army Empty = new Army();
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

    public static Army operator +(int a, Army b) =>
        new()
        {
            Triangle = (short)(a + b.Triangle),
            Circle = (short)(a + b.Circle),
            Square = (short)(a + b.Square),
            Dot = (short)(a + b.Dot)
        };

    public static Army operator -(Army a, Army b) =>
        new()
        {
            Triangle = (short)(a.Triangle - b.Triangle),
            Circle = (short)(a.Circle - b.Circle),
            Square = (short)(a.Square - b.Square),
            Dot = (short)(a.Dot - b.Dot)
        };

    public static int operator *(Army a, Army b) =>
        a.Dot * b.Dot +
        a.Circle * b.Circle +
        a.Triangle * b.Triangle +
        a.Square * b.Square;

    public static Army operator *(int a, Army b) =>
        new Army()
        {
            Triangle = (short)(a * b.Triangle),
            Circle = (short)(a * b.Circle),
            Square = (short)(a * b.Square),
            Dot = (short)(a * b.Dot)
        };

    public static Army operator -(Army army)
        => -1 * army;

    public override string ToString() =>
        $"Army(Triangles: {Triangle}, Circles: {Circle}, Squares: {Square}, Dots: {Dot})";

    public short this[Unit unit] => unit switch
    {
        Unit.Dot => Dot,
        Unit.Triangle => Triangle,
        Unit.Circle => Circle,
        Unit.Square => Square,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };

    public static Army FromUnit(Unit unit, short count)
    {
        return unit switch
        {
            Unit.Dot => new Army() { Dot = count },
            Unit.Triangle => new Army() { Triangle = count },
            Unit.Circle => new Army() { Circle = count },
            Unit.Square => new Army() { Square = count },
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    public double GetStrengthOver(Army opponent, Army exponent)
    {
        var strength =
            Math.Pow(Math.Max(0, Triangle - opponent.Square), exponent.Triangle / 100) +
            Math.Pow(Math.Max(0, Square - opponent.Circle), exponent.Square / 100) +
            Math.Pow(Math.Max(0, Circle - opponent.Triangle), exponent.Circle / 100);
        return strength;
    }

    public Army MultiplyAndRoundUp(double fraction)
    {
        return new Army()
        {
            Triangle = (short)Math.Ceiling(fraction * Triangle),
            Square = (short)Math.Ceiling(fraction * Square),
            Circle = (short)Math.Ceiling(fraction * Circle),
            Dot = (short)Math.Ceiling(fraction * Dot),
        };
    }

    public static Army Min(Army a, Army b)
    {
        return new Army()
        {
            Triangle = (short)Math.Min(a.Triangle, b.Triangle),
            Circle = (short)Math.Min(a.Circle, b.Circle),
            Square = (short)Math.Min(a.Square, b.Square),
            Dot = (short)Math.Min(a.Dot, b.Dot)
        };
    }
}