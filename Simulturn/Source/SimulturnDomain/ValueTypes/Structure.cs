using SimulturnDomain.Enums;
using System.Numerics;

namespace SimulturnDomain.ValueTypes;
public readonly struct Structure : IAdditionOperators<Structure, Structure, Structure>, ISubtractionOperators<Structure, Structure, Structure>
{
    public static readonly Structure Empty = new Structure();
    public Structure(short root = 0,
        short cube = 0,
        short pyramid = 0,
        short sphere = 0,
        short plane = 0,
        short axis = 0)
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

    public short this[Building structureId]
    {
        get
        {
            return structureId switch
            {
                Building.Root => Root,
                Building.Cube => Cube,
                Building.Pyramid => Pyramid,
                Building.Sphere => Sphere,
                Building.Plane => Plane,
                Building.Axis => Axis,
                _ => throw new ArgumentOutOfRangeException(nameof(structureId)),
            };
        }
    }

    public Structure Add(Building unit, short count)
    {
        return unit switch
        {
            Building.Root => new Structure(Convert.ToInt16(Root + count), Cube, Pyramid, Sphere, Plane, Axis),
            Building.Cube => new Structure(Root, Convert.ToInt16(Cube + count), Pyramid, Sphere, Plane, Axis),
            Building.Pyramid => new Structure(Root, Cube, Convert.ToInt16(Pyramid + count), Sphere, Plane, Axis),
            Building.Sphere => new Structure(Root, Cube, Pyramid, Convert.ToInt16(Sphere + count), Plane, Axis),
            Building.Plane => new Structure(Root, Cube, Pyramid, Sphere, Convert.ToInt16(Plane + count), Axis),
            Building.Axis => new Structure(Root, Cube, Pyramid, Sphere, Plane, Convert.ToInt16(Axis + count)),
            _ => throw new NotImplementedException()
        };
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

    public bool EachBuildingIsSmallerOrEqualThan(Structure other)
    {
        return Root <= other.Root &&
            Cube <= other.Cube &&
            Pyramid <= other.Pyramid &&
            Sphere <= other.Sphere &&
            Plane <= other.Plane &&
            Axis <= other.Axis;
    }

    public bool AnyBuildingIsSmallerThan(Structure other)
    {
        return Root < other.Root ||
            Cube < other.Cube ||
            Pyramid < other.Pyramid ||
            Sphere < other.Sphere ||
            Plane < other.Plane ||
            Axis < other.Axis;
    }

    public bool EachBuildingIsGreaterOrEqualThan(Structure other)
    {
        return Root >= other.Root &&
            Cube >= other.Cube &&
            Pyramid >= other.Pyramid &&
            Sphere >= other.Sphere &&
            Plane >= other.Plane &&
            Axis >= other.Axis;
    }

    public bool AnyBuildingIsGreaterThan(Structure other)
    {
        return Root > other.Root ||
            Cube > other.Cube ||
            Pyramid > other.Pyramid ||
            Sphere > other.Sphere ||
            Plane > other.Plane ||
            Axis > other.Axis;
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

    public bool Any()
    {
        return Root > 0 ||
            Cube > 0 ||
            Pyramid > 0 ||
            Sphere > 0 ||
            Plane > 0 ||
            Axis > 0;
    }

    public static Structure FromBuildings(IReadOnlyDictionary<Building, short> buildings)
    {
        return new Structure()
        {
            Root = buildings.ContainsKey(Building.Root) ? buildings[Building.Root] : (short)0,
            Cube = buildings.ContainsKey(Building.Cube) ? buildings[Building.Cube] : (short)0,
            Pyramid = buildings.ContainsKey(Building.Pyramid) ? buildings[Building.Pyramid] : (short)0,
            Sphere = buildings.ContainsKey(Building.Sphere) ? buildings[Building.Sphere] : (short)0,
            Plane = buildings.ContainsKey(Building.Plane) ? buildings[Building.Plane] : (short)0,
            Axis = buildings.ContainsKey(Building.Axis) ? buildings[Building.Axis] : (short)0,
        };
    }

    public override string ToString()
    {
        return $"{Root},{Cube},{Pyramid},{Sphere},{Plane},{Axis}";
    }
}