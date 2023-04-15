using System.Diagnostics;

namespace SimulturnDomain.Entities;
public readonly struct Army
{
    public Army(short triangle = 0, short square = 0, short circle = 0, short line = 0, short point = 0)
    {
        Triangle = triangle;
        Square = square;
        Circle = circle;
        Line = line;
        Point = point;
    }
    public short Point { get; init; }

    public short Triangle { get; init; }
    public short Square { get; init; }
    public short Circle { get; init; }
    public short Line { get; init; }

    public static Army operator +(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToInt16(a.Triangle + b.Triangle),
            Square = Convert.ToInt16(a.Square + b.Square),
            Circle = Convert.ToInt16(a.Circle + b.Circle),
            Line = Convert.ToInt16(a.Line + b.Line),
            Point = Convert.ToInt16(a.Point + b.Point)
        };
    }

    public static Army operator -(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToInt16(a.Triangle - b.Triangle),
            Square = Convert.ToInt16(a.Square - b.Square),
            Circle = Convert.ToInt16(a.Circle - b.Circle),
            Line = Convert.ToInt16(a.Line - b.Line),
            Point = Convert.ToInt16(a.Point - b.Point)
        };
    }

    public static Army operator *(Army army, ushort value)
    {
        return new Army()
        {
            Triangle = Convert.ToInt16(army.Triangle * value),
            Square = Convert.ToInt16(army.Square * value),
            Circle = Convert.ToInt16(army.Circle * value),
            Line = Convert.ToInt16(army.Line * value),
            Point = Convert.ToInt16(army.Point * value)
        };
    }

    public static Army operator *(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToInt16(a.Triangle * b.Triangle),
            Square = Convert.ToInt16(a.Square * b.Square),
            Circle = Convert.ToInt16(a.Circle * b.Circle),
            Line = Convert.ToInt16(a.Line * b.Line),
            Point = Convert.ToInt16(a.Point * b.Point)
        };
    }

    public static Army operator *(Army army, Structure structures)
    {
        return new Army()
        {
            Triangle = structures[army.Triangle],
            Square = structures[army.Square],
            Circle = structures[army.Circle],
            Line = structures[army.Line],
            Point = structures[army.Point]
        };
    }

    public Army Max(Army max)
    {
        return new Army()
        {
            Triangle = Triangle > max.Triangle ? max.Triangle : Triangle,
            Square = Square > max.Square ? max.Square : Square,
            Circle = Circle > max.Circle ? max.Circle : Circle,
            Line = Line > max.Line ? max.Line : Line,
            Point = Point > max.Point ? max.Point : Point,
        };
    }

    public bool Any()
    {
        return Point > 0 ||
            Line > 0 ||
            Circle > 0 ||
            Square > 0 ||
            Triangle > 0;
    }

    public short Sum()
    {
        return Convert.ToInt16(Triangle + Square + Circle + Line + Point);
    }

    public ushort GetStrengthOver(Army army, double exponent)
    {
        ushort strength = (ushort)(Math.Max(0, Triangle - army.Square) +
            Math.Max(0, Square - army.Circle) +
            Math.Max(0, Circle - army.Triangle));
        strength = (ushort)Math.Pow(strength, exponent);
        return strength;
    }
}
