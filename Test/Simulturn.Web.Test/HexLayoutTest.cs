using Simulturn.Core.Model;
using Simulturn.Web.Board;
using System.Globalization;

namespace Simulturn.Web.Test;

public class HexLayoutTest
{
    [Test]
    public void Svg_UsesInvariantCulture_EvenUnderGermanCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            HexLayout.Svg(1.5).ShouldBe("1.5");
            HexLayout.UnitHexPath.ShouldNotContain(",");
            HexLayout.ArrowPath(new Hexagon(0, 0), new Hexagon(1, 0)).ShouldNotContain(",");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Center_MatchesPrinterConvention()
    {
        // Printer.cs: cx = √3·S·(X + Z/2), cy = 1.5·S·Z with S = 100.
        var hexagon = new Hexagon(2, -1); // Z = -1
        (double x, double y) = HexLayout.Center(hexagon);
        x.ShouldBe(Math.Sqrt(3) * 100 * (2 + -1 / 2.0), tolerance: 1e-9);
        y.ShouldBe(1.5 * 100 * -1, tolerance: 1e-9);
    }

    [Test]
    public void Center_OfOrigin_IsZero()
    {
        (double x, double y) = HexLayout.Center(new Hexagon(0, 0));
        x.ShouldBe(0);
        y.ShouldBe(0);
    }

    [Test]
    public void Bounds_ContainAllHexCenters()
    {
        Hexagon[] hexagons = [new(0, 0), new(3, -1), new(-2, 2)];
        var bounds = HexLayout.Bounds(hexagons);
        foreach (var hexagon in hexagons)
        {
            (double x, double y) = HexLayout.Center(hexagon);
            (x >= bounds.MinX && x <= bounds.MinX + bounds.Width).ShouldBeTrue();
            (y >= bounds.MinY && y <= bounds.MinY + bounds.Height).ShouldBeTrue();
        }
    }

    [Test]
    public void ArrowPaths_ForOpposingDirections_Differ()
    {
        var a = new Hexagon(0, 0);
        var b = new Hexagon(2, 0);
        // The bow is always perpendicular-left of travel, so A→B and B→A bow apart.
        HexLayout.ArrowPath(a, b).ShouldNotBe(HexLayout.ArrowPath(b, a));
    }
}
