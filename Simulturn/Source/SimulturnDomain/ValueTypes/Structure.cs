using SimulturnDomain.Enums;
using System.Numerics;

namespace SimulturnDomain.ValueTypes;
public readonly struct Structure : IAdditionOperators<Structure, Structure, Structure>, ISubtractionOperators<Structure, Structure, Structure>
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

    public short this[StructureId structureId]
    {
        get
        {
            return structureId switch
            {
                StructureId.Root => Root,
                StructureId.Cube => Cube,
                StructureId.Pyramid => Pyramid,
                StructureId.Sphere => Sphere,
                StructureId.Plane => Plane,
                StructureId.Axis => Axis,
                _ => throw new ArgumentOutOfRangeException(nameof(structureId)),
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

    public static Structure operator +(Structure structure, ushort b)
    {
        return new Structure()
        {
            Root = Convert.ToInt16(structure.Root + b),
            Cube = Convert.ToInt16(structure.Cube + b),
            Pyramid = Convert.ToInt16(structure.Pyramid + b),
            Sphere = Convert.ToInt16(structure.Sphere + b),
            Plane = Convert.ToInt16(structure.Plane + b),
            Axis = Convert.ToInt16(structure.Axis + b)
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

    public static Structure Combine(Structure a, Structure b, Func<short, short, short> func)
    {
        return new Structure()
        {
            Root = func(a.Root, b.Root),
            Cube = func(a.Cube, b.Cube),
            Pyramid = func(a.Pyramid, b.Pyramid),
            Sphere = func(a.Sphere, b.Sphere),
            Plane = func(a.Plane, b.Plane),
            Axis = func(a.Axis, b.Axis),
        };
    }

    public short Sum()
    {
        return Convert.ToInt16(Root + Cube + Pyramid + Sphere + Plane + Axis);
    }
}
