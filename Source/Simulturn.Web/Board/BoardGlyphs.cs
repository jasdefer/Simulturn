using Simulturn.Core.Model;

namespace Simulturn.Web.Board;

/// <summary>Shared glyph lookups for board rendering.</summary>
public static class BoardGlyphs
{
    public static string SymbolId(Unit unit) => unit switch
    {
        Unit.Triangle => "u-triangle",
        Unit.Circle => "u-circle",
        Unit.Square => "u-square",
        Unit.Dot => "u-dot",
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };

    public static string SymbolId(Building building) => building switch
    {
        Building.Plane => "b-plane",
        Building.Axis => "b-axis",
        Building.Dome => "b-dome",
        Building.Pyramid => "b-pyramid",
        Building.Cube => "b-cube",
        _ => throw new ArgumentOutOfRangeException(nameof(building), building, null),
    };

    public static char TextGlyph(Unit unit) => unit switch
    {
        Unit.Triangle => '△',
        Unit.Circle => '○',
        Unit.Square => '□',
        Unit.Dot => '•',
        _ => '?',
    };

    public static string DisplayName(Unit unit) => unit switch
    {
        Unit.Triangle => "Triangle",
        Unit.Circle => "Circle",
        Unit.Square => "Square",
        Unit.Dot => "Dot (worker)",
        _ => unit.ToString(),
    };

    public static string DisplayName(Building building) => building switch
    {
        Building.Plane => "Plane (HQ)",
        Building.Axis => "Axis (supply)",
        Building.Dome => "Dome",
        Building.Pyramid => "Pyramid",
        Building.Cube => "Cube",
        _ => building.ToString(),
    };

    public static IEnumerable<Unit> Units { get; } = [Unit.Dot, Unit.Triangle, Unit.Circle, Unit.Square];

    public static IEnumerable<Building> Buildings { get; } = [Building.Plane, Building.Axis, Building.Dome, Building.Pyramid, Building.Cube];

    /// <summary>Compact one-line army label like "△3 ○2 •5" for arrow pills and tooltips.</summary>
    public static string CompactLabel(Army army)
    {
        List<string> parts = [];
        foreach (var unit in Units)
        {
            if (army[unit] != 0)
            {
                parts.Add($"{TextGlyph(unit)}{army[unit]}");
            }
        }
        return string.Join(" ", parts);
    }
}
