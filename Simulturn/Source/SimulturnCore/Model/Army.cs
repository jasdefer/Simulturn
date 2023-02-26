namespace SimulturnCore.Model;
public readonly struct Army
{
    public Army(ushort triangle = 0, ushort square = 0, ushort circle = 0, ushort line = 0, ushort point = 0)
    {
        Triangle = triangle;
        Square = square;
        Circle = circle;
        Line = line;
        Point = point;
    }
    public ushort Point { get; init; }

    public ushort Triangle { get; init; }
    public ushort Square { get; init; }
    public ushort Circle { get; init; }
    public ushort Line { get; init; }

    public static Army operator +(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToUInt16(a.Triangle + b.Triangle),
            Square = Convert.ToUInt16(a.Square + b.Square),
            Circle = Convert.ToUInt16(a.Circle + b.Circle),
            Line = Convert.ToUInt16(a.Line + b.Line)
        };
    }

    public static Army operator -(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToUInt16(a.Triangle - b.Triangle),
            Square = Convert.ToUInt16(a.Square - b.Square),
            Circle = Convert.ToUInt16(a.Circle - b.Circle),
            Line = Convert.ToUInt16(a.Line - b.Line)
        };
    }

    public static Army operator *(Army a, ushort value)
    {
        return new Army()
        {
            Triangle = Convert.ToUInt16(a.Triangle * value),
            Square = Convert.ToUInt16(a.Square * value),
            Circle = Convert.ToUInt16(a.Circle * value),
            Line = Convert.ToUInt16(a.Line * value)
        };
    }

    public static Army operator *(Army a, Army b)
    {
        return new Army()
        {
            Triangle = Convert.ToUInt16(a.Triangle * b.Triangle),
            Square = Convert.ToUInt16(a.Square * b.Square),
            Circle = Convert.ToUInt16(a.Circle * b.Circle),
            Line = Convert.ToUInt16(a.Line * b.Line)
        };
    }

    public static Army operator *(Army army, Structure structures)
    {
        return new Army()
        {
            Triangle = structures[army.Triangle],
            Square = structures[army.Square],
            Circle = structures[army.Circle],
            Line = structures[army.Line]
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
        };
    }

    public ushort Sum()
    {
        return Convert.ToUInt16(Triangle + Square + Circle + Line);
    }
}
