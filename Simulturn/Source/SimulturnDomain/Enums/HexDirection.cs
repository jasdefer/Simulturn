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
    public static readonly ImmutableHashSet<HexDirection> All = Enum.GetValues<HexDirection>().ToImmutableHashSet();
}