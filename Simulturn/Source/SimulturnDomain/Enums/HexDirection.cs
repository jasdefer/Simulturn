using System.Collections.Immutable;

namespace SimulturnDomain.Enums;
public enum HexDirection
{
    NorthEast,
    East,
    SouthEast,
    SouthWest,
    West,
    NorthWest
}

public static class HexDirections
{
    public static HexDirection Opposit(HexDirection hexDirection)
    {
        return hexDirection switch
        {

            HexDirection.NorthEast => HexDirection.SouthWest,
            HexDirection.East => HexDirection.West,
            HexDirection.SouthEast => HexDirection.NorthWest,
            HexDirection.SouthWest => HexDirection.NorthEast,
            HexDirection.West => HexDirection.East,
            HexDirection.NorthWest => HexDirection.SouthEast,
            _ => throw new ArgumentException(nameof(hexDirection)),
        };
    }
    public static readonly ImmutableHashSet<HexDirection> All = Enum.GetValues<HexDirection>().ToImmutableHashSet();
}