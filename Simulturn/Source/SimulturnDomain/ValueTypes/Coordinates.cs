namespace SimulturnDomain.ValueTypes;
public readonly struct Coordinates
{
    public Coordinates(ushort x, ushort y, ushort z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public ushort X { get; init; }
    public ushort Y { get; init; }
    public ushort Z { get; init; }
}