using SimulturnDomain.Enums;

namespace SimulturnDomain.Entities;
public readonly struct Structure
{
    public Structure(short root,
        short cube,
        short pyramid,
        short sphere,
        short plane)
    {
        Root = root;
        Cube = cube;
        Pyramid = pyramid;
        Sphere = sphere;
        Plane = plane;
    }

    public short Root { get; init; }
    public short Cube { get; init; }
    public short Pyramid { get; init; }
    public short Sphere { get; init; }
    public short Plane { get; init; }

    public short this[short index]
    {
        get
        {
            return index switch
            {
                (short)StructureIds.Root => Root,
                (short)StructureIds.Cube => Cube,
                (short)StructureIds.Pyramid => Pyramid,
                (short)StructureIds.Sphere => Sphere,
                (short)StructureIds.Plane => Plane,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }
    }
    public static Structure operator *(Structure a, Structure b)
    {
        return new Structure()
        {
            Root = Convert.ToInt16(a.Root * b.Root),
            Cube = Convert.ToInt16(a.Cube * b.Cube),
            Pyramid = Convert.ToInt16(a.Pyramid * b.Pyramid),
            Sphere = Convert.ToInt16(a.Sphere * b.Sphere),
            Plane = Convert.ToInt16(a.Plane * b.Plane)
        };
    }

    public static Structure operator +(Structure a, Structure b)
    {
        return new Structure()
        {
            Root = Convert.ToInt16(a.Root + b.Root),
            Cube = Convert.ToInt16(a.Cube + b.Cube),
            Pyramid = Convert.ToInt16(a.Pyramid + b.Pyramid),
            Sphere = Convert.ToInt16(a.Sphere + b.Sphere),
            Plane = Convert.ToInt16(a.Plane + b.Plane)
        };
    }

    public static Structure operator -(Structure a, Structure b)
    {
        return new Structure()
        {
            Root = Convert.ToInt16(a.Root - b.Root),
            Cube = Convert.ToInt16(a.Cube - b.Cube),
            Pyramid = Convert.ToInt16(a.Pyramid - b.Pyramid),
            Sphere = Convert.ToInt16(a.Sphere - b.Sphere),
            Plane = Convert.ToInt16(a.Plane - b.Plane)
        };
    }

    public short Sum()
    {
        return Convert.ToInt16(Root + Cube + Pyramid + Sphere + Plane);
    }
}
