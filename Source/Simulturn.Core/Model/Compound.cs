using System.Numerics;

namespace Simulturn.Core.Model;

public readonly struct Compound : IAdditionOperators<Compound, Compound, Compound>
{
    public static readonly Compound Empty = new Compound();
    /// <summary>
    /// Produces circles
    /// </summary>
    public short Dome { get; init; }

    /// <summary>
    /// Produces Triangles
    /// </summary>
    public short Pyramid { get; init; }

    /// <summary>
    /// Produces squares
    /// </summary>
    public short Cube { get; init; }

    /// <summary>
    /// Produces dots and enables resource gathering
    /// </summary>
    public short Plane { get; init; }

    /// <summary>
    /// Provides spaces for units
    /// </summary>
    public short Axis { get; init; }

    public bool IsEmpty => Dome == 0 && Pyramid == 0 && Cube == 0 && Plane == 0 && Axis == 0;

    public int Sum() => Dome + Axis + Dome + Pyramid + Cube;

    public static Compound operator +(Compound a, Compound b) =>
        new Compound()
        {
            Dome = (short)(a.Dome + b.Dome),
            Pyramid = (short)(a.Pyramid + b.Pyramid),
            Cube = (short)(a.Cube + b.Cube),
            Plane = (short)(a.Plane + b.Plane),
            Axis = (short)(a.Axis + b.Axis)
        };

    public static int operator *(Compound a, Compound b) =>
        a.Dome * b.Dome +
        a.Pyramid * b.Pyramid +
        a.Cube * b.Cube +
        a.Plane * b.Plane +
        a.Axis * b.Axis;

    public static Compound operator *(int a, Compound b) =>
        new Compound()
        {
            Dome = (short)(a * b.Dome),
            Pyramid = (short)(a * b.Pyramid),
            Cube = (short)(a * b.Cube),
            Plane = (short)(a * b.Plane),
            Axis = (short)(a * b.Axis)
        };

    public static Compound operator -(Compound compound)
        => -1 * compound;

    public override string ToString() =>
        $"Army(Plane: {Plane}, Axis: {Axis}, Dome: {Dome}, Pyramid: {Pyramid}, Cube: {Cube})";

    public short this[Building building] => building switch
    {
        Building.Plane => Plane,
        Building.Axis => Axis,
        Building.Dome => Dome,
        Building.Pyramid => Pyramid,
        Building.Cube => Cube,
        _ => throw new ArgumentOutOfRangeException(nameof(building), building, null)
    };

    public static Compound FromBuilding(Building building, short count)
    {
        return building switch
        {
            Building.Plane => new Compound() { Plane = count },
            Building.Axis => new Compound() { Axis = count },
            Building.Dome => new Compound() { Dome = count },
            Building.Pyramid => new Compound() { Pyramid = count },
            Building.Cube => new Compound() { Cube = count },
            _ => throw new ArgumentOutOfRangeException(nameof(building), building, null)
        };
    }
}