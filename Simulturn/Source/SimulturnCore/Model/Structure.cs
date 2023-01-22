namespace SimulturnCore.Model;
public readonly struct Structure
{
    public Structure(ushort root, ushort cube, ushort pyramid, ushort sphere, ushort plane)
    {
        Root = root;
        Cube = cube;
        Pyramid = pyramid;
        Sphere = sphere;
        Plane = plane;
    }

    public ushort Root { get; init; }
    public ushort Cube { get; init; }
    public ushort Pyramid { get; init; }
    public ushort Sphere { get; init; }
    public ushort Plane { get; init; }

    public static Structure operator *(Structure a, Structure b)
    {
        return new Structure()
        {
            Root = Convert.ToUInt16(a.Root * b.Root),
            Cube = Convert.ToUInt16(a.Cube * b.Cube),
            Pyramid = Convert.ToUInt16(a.Pyramid * b.Pyramid),
            Sphere = Convert.ToUInt16(a.Sphere * b.Sphere),
            Plane = Convert.ToUInt16(a.Plane * b.Plane)
        };
    }

    public ushort Sum()
    {
        return Convert.ToUInt16(Root + Cube + Pyramid + Sphere + Plane);
    }
}
