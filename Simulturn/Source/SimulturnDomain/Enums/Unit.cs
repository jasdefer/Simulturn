using System.Collections.Immutable;

namespace SimulturnDomain.Enums;
public enum Unit
{
    Triangle,
    Square,
    Circle,
    Line,
    Point
}

public static class Units
{
    public static ImmutableHashSet<Unit> All = Enum.GetValues<Unit>().ToImmutableHashSet();
}