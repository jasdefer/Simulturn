using Simulturn.Core.Model;
using System.Globalization;
using System.Text;

namespace Simulturn.Web.Board;

/// <summary>
/// Pure hex-grid geometry for the SVG board. Pointy-top orientation matching
/// <c>Simulturn.Core.Helper.Printer</c>: cx = √3·S·(X + Z/2), cy = 1.5·S·Z.
/// All numbers are formatted with <see cref="CultureInfo.InvariantCulture"/> —
/// SVG attributes must never contain culture-specific decimal separators.
/// </summary>
public static class HexLayout
{
    /// <summary>Hexagon outer radius in viewBox units. Fixed; the SVG viewBox does all scaling.</summary>
    public const double Size = 100;

    /// <summary>Corner radius used for the tile path; slightly inset to create a visual gutter between tiles.</summary>
    public const double TileRadius = Size * 0.965;

    private static readonly double _sqrt3 = Math.Sqrt(3);

    /// <summary>Path of a unit hexagon centered on the origin. Tiles translate this via a transform.</summary>
    public static string UnitHexPath { get; } = BuildHexPath(TileRadius);

    public static (double X, double Y) Center(Hexagon hexagon)
    {
        double cx = _sqrt3 * Size * (hexagon.X + hexagon.Z / 2.0);
        double cy = 1.5 * Size * hexagon.Z;
        return (cx, cy);
    }

    public static BoardBounds Bounds(IEnumerable<Hexagon> hexagons, double margin = 8)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var hexagon in hexagons)
        {
            (double cx, double cy) = Center(hexagon);
            minX = Math.Min(minX, cx);
            minY = Math.Min(minY, cy);
            maxX = Math.Max(maxX, cx);
            maxY = Math.Max(maxY, cy);
        }
        if (minX > maxX)
        {
            return new BoardBounds(0, 0, 1, 1);
        }
        double halfWidth = _sqrt3 * 0.5 * Size + margin;
        double halfHeight = Size + margin;
        return new BoardBounds(
            minX - halfWidth,
            minY - halfHeight,
            maxX - minX + 2 * halfWidth,
            maxY - minY + 2 * halfHeight);
    }

    /// <summary>Formats a coordinate for SVG attributes, culture-invariant.</summary>
    public static string Svg(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Translate(Hexagon hexagon)
    {
        (double cx, double cy) = Center(hexagon);
        return $"translate({Svg(cx)} {Svg(cy)})";
    }

    /// <summary>
    /// Quadratic Bézier path between two hex centers for order arrows. Endpoints are pulled
    /// inside the hex borders; the control point bows perpendicular-left of the travel direction,
    /// so opposing arrows (A→B and B→A) bow to opposite sides automatically.
    /// </summary>
    public static string ArrowPath(Hexagon from, Hexagon to, double laneOffset = 0)
    {
        var geometry = ArrowGeometry(from, to, laneOffset);
        if (geometry is null)
        {
            return string.Empty;
        }
        var (start, control, end) = geometry.Value;
        return $"M {Svg(start.X)} {Svg(start.Y)} Q {Svg(control.X)} {Svg(control.Y)} {Svg(end.X)} {Svg(end.Y)}";
    }

    /// <summary>Point on the arrow path at t = 0.5 (label anchor).</summary>
    public static (double X, double Y) ArrowMidpoint(Hexagon from, Hexagon to, double laneOffset = 0)
    {
        var geometry = ArrowGeometry(from, to, laneOffset);
        if (geometry is null)
        {
            return Center(from);
        }
        var (start, control, end) = geometry.Value;
        // Quadratic Bézier at t = 0.5: B = 0.25·P0 + 0.5·C + 0.25·P1.
        return (0.25 * start.X + 0.5 * control.X + 0.25 * end.X,
                0.25 * start.Y + 0.5 * control.Y + 0.25 * end.Y);
    }

    private static ((double X, double Y) Start, (double X, double Y) Control, (double X, double Y) End)? ArrowGeometry(
        Hexagon from, Hexagon to, double laneOffset)
    {
        (double x1, double y1) = Center(from);
        (double x2, double y2) = Center(to);
        double dx = x2 - x1;
        double dy = y2 - y1;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-6)
        {
            return null;
        }
        double ux = dx / length;
        double uy = dy / length;

        // Pull endpoints inside the tile borders.
        double inset = Size * 0.72;
        var start = (X: x1 + ux * inset, Y: y1 + uy * inset);
        var end = (X: x2 - ux * inset, Y: y2 - uy * inset);

        // Control point bows perpendicular-left of the travel direction.
        double bow = 0.12 * length + laneOffset;
        var control = (X: (start.X + end.X) / 2 - uy * bow, Y: (start.Y + end.Y) / 2 + ux * bow);
        return (start, control, end);
    }

    private static string BuildHexPath(double radius)
    {
        StringBuilder sb = new();
        for (int i = 0; i < 6; i++)
        {
            double angle = Math.PI / 180.0 * (60 * i - 30);
            double x = radius * Math.Cos(angle);
            double y = radius * Math.Sin(angle);
            sb.Append(i == 0 ? "M " : "L ");
            sb.Append(Svg(x));
            sb.Append(' ');
            sb.Append(Svg(y));
            sb.Append(' ');
        }
        sb.Append('Z');
        return sb.ToString();
    }
}

/// <summary>Bounding box of the board in viewBox units.</summary>
public sealed record BoardBounds(double MinX, double MinY, double Width, double Height)
{
    public string ViewBox =>
        $"{HexLayout.Svg(MinX)} {HexLayout.Svg(MinY)} {HexLayout.Svg(Width)} {HexLayout.Svg(Height)}";
}
