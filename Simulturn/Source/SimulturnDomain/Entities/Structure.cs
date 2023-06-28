using SimulturnDomain.Enums;

namespace SimulturnDomain.Entities;
public readonly struct Structure
{
    public Structure(short root,
        short cube,
        short pyramid,
        short sphere,
        short plane,
        short axis)
    {
        Root = root;
        Cube = cube;
        Pyramid = pyramid;
        Sphere = sphere;
        Plane = plane;
        Axis = axis;
    }

    /// <summary>
    /// Head quater to produce points and gather matter
    /// </summary>
    public short Root { get; init; }

    /// <summary>
    /// Produces squares
    /// </summary>
    public short Cube { get; init; }

    /// <summary>
    /// Produces triangles
    /// </summary>
    public short Pyramid { get; init; }

    /// <summary>
    /// Produces circles
    /// </summary>
    public short Sphere { get; init; }

    /// <summary>
    /// Produces lines
    /// </summary>
    public short Plane { get; init; }

    /// <summary>
    /// Provides space
    /// </summary>
    public short Axis { get; init; }

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
                (short)StructureIds.Axis => Axis,
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
            Plane = Convert.ToInt16(a.Plane * b.Plane),
            Axis = Convert.ToInt16(a.Axis * b.Axis),
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
            Plane = Convert.ToInt16(a.Plane + b.Plane),
            Axis = Convert.ToInt16(a.Axis + b.Axis)
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
            Plane = Convert.ToInt16(a.Plane - b.Plane),
            Axis = Convert.ToInt16(a.Axis - b.Axis)
        };
    }

    public short Sum()
    {
        return Convert.ToInt16(Root + Cube + Pyramid + Sphere + Plane + Axis);
    }
}
