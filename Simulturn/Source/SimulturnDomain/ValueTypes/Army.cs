﻿using SimulturnDomain.Enums;
using System.Numerics;

namespace SimulturnDomain.ValueTypes;
public readonly struct Army : IAdditionOperators<Army, Army, Army>, ISubtractionOperators<Army, Army, Army>
{
    public static readonly Army Empty = new Army();
    public Army(short triangle = 0, short square = 0, short circle = 0, short line = 0, short point = 0)
    {
        Triangle = triangle;
        Square = square;
        Circle = circle;
        Line = line;
        Point = point;
    }

    public short Triangle { get; init; }
    public short Square { get; init; }
    public short Circle { get; init; }
    public short Line { get; init; }
    public short Point { get; init; }

    public short this[Unit unit] => GetUnitCount(unit);

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

    public short GetUnitCount(Unit unit)
    {
        return unit switch
        {
            Unit.Triangle => Triangle,
            Unit.Square => Square,
            Unit.Circle => Circle,
            Unit.Line => Line,
            Unit.Point => Point,
            _ => throw new NotImplementedException()
        };
    }

    public Army Add(Unit unit, short count)
    {
        return unit switch
        {
            Unit.Triangle => new Army(Convert.ToInt16(Triangle + count), Square, Circle, Line, Point),
            Unit.Square => new Army(Triangle, Convert.ToInt16(Square + count), Circle, Line, Point),
            Unit.Circle => new Army(Triangle, Square, Convert.ToInt16(Circle + count), Line, Point),
            Unit.Line => new Army(Triangle, Square, Circle, Convert.ToInt16(Line + count), Point),
            Unit.Point => new Army(Triangle, Square, Circle, Line, Convert.ToInt16(Point + count)),
            _ => throw new NotImplementedException()
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

    public static bool operator <(Army a, Army b)
    {
        return a.Triangle < b.Triangle &&
            a.Square < b.Square &&
            a.Circle < b.Circle &&
            a.Line < b.Line &&
            a.Point < b.Point;
    }

    public static bool operator >(Army a, Army b)
    {
        return a.Triangle > b.Triangle &&
            a.Square > b.Square &&
            a.Circle > b.Circle &&
            a.Line > b.Line &&
            a.Point > b.Point;
    }

    public static bool operator <=(Army a, Army b)
    {
        return a.Triangle <= b.Triangle &&
            a.Square <= b.Square &&
            a.Circle <= b.Circle &&
            a.Line <= b.Line &&
            a.Point <= b.Point;
    }

    public static bool operator >=(Army a, Army b)
    {
        return a.Triangle >= b.Triangle &&
            a.Square >= b.Square &&
            a.Circle >= b.Circle &&
            a.Line >= b.Line &&
            a.Point >= b.Point;
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

    public bool EachUnitIsSmallerOrEqualThan(Army other)
    {
        return Triangle <= other.Triangle &&
            Square <= other.Square &&
            Circle <= other.Circle &&
            Line <= other.Line &&
            Point <= other.Point;
    }

    public bool AnyUnitIsSmallerThan(Army other)
    {
        return Triangle < other.Triangle ||
            Square < other.Square ||
            Circle < other.Circle ||
            Line < other.Line ||
            Point < other.Point;
    }

    public bool EachUnitIsGreaterOrEqualThan(Army other)
    {
        return Triangle >= other.Triangle &&
            Square >= other.Square &&
            Circle >= other.Circle &&
            Line >= other.Line &&
            Point >= other.Point;
    }

    public bool AnyUnitIsGreaterThan(Army other)
    {
        return Triangle > other.Triangle ||
            Square > other.Square ||
            Circle > other.Circle ||
            Line > other.Line ||
            Point > other.Point;
    }

    public bool Any()
    {
        return Triangle > 0 ||
            Square > 0 ||
            Circle > 0 ||
            Line > 0 ||
            Point > 0;
    }

    public short Sum()
    {
        return Convert.ToInt16(Triangle + Square + Circle + Line + Point);
    }

    public double GetStrengthOver(Army opponent, double exponent)
    {
        var strength =
            Math.Pow(Math.Max(0, Triangle - opponent.Square), exponent) +
            Math.Pow(Math.Max(0, Square - opponent.Circle), exponent) +
            Math.Pow(Math.Max(0, Circle - opponent.Triangle), exponent);
        return strength;
    }

    public Army MultiplyAndRoundUp(double fraction)
    {
        return new Army()
        {
            Triangle = (short)Math.Ceiling(fraction * Triangle),
            Square = (short)Math.Ceiling(fraction * Square),
            Circle = (short)Math.Ceiling(fraction * Circle),
            Line = (short)Math.Ceiling(fraction * Line),
            Point = (short)Math.Ceiling(fraction * Point),
        };
    }

    public static Army FromUnits(IReadOnlyDictionary<Unit, short> units)
    {
        return new Army()
        {
            Triangle = units.ContainsKey(Unit.Triangle) ? units[Unit.Triangle] : (short)0,
            Square = units.ContainsKey(Unit.Square) ? units[Unit.Square] : (short)0,
            Circle = units.ContainsKey(Unit.Circle) ? units[Unit.Circle] : (short)0,
            Line = units.ContainsKey(Unit.Line) ? units[Unit.Line] : (short)0,
            Point = units.ContainsKey(Unit.Point) ? units[Unit.Point] : (short)0,
        };
    }

    public override string ToString()
    {
        return $"{Triangle},{Square},{Circle},{Line},{Point}";
    }
}
