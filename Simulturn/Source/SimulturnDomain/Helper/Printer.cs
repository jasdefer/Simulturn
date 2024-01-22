using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;
using System.Text;

namespace SimulturnDomain.Helper;
public static class Printer
{
    public static void Print(string filePath, HexMap<ushort> map, double radius = 100)
    {
        using var writer = new StreamWriter(filePath, false);
        var minX = map.Keys.Min(x => MapX(x.X, x.Y, radius)) - radius;
        var maxX = map.Keys.Max(x => MapX(x.X, x.Y, radius)) + radius;
        var minY = map.Keys.Min(x => MapY(x.Y, radius)) - radius;
        var maxY = map.Keys.Max(x => MapY(x.Y, radius)) + radius;
        writer.WriteLine($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{0} {0} {maxX - minX} {maxY - minY}'>");
        writer.WriteLine("<style>");
        writer.WriteLine("  .hexagon { fill: none; stroke: black; stroke-width: 1; }");
        writer.WriteLine("  .resource { font-family: Verdana; font-size: 20px; fill: black; text-anchor: middle; alignment-baseline: central; }");
        writer.WriteLine("</style>");
        foreach (var coordinates in map.Keys)
        {
            string hexagonPath = DrawHexagon(coordinates, 100, minX, minY);
            writer.WriteLine($"<path d='{hexagonPath}' class='hexagon'/>");
            writer.WriteLine($"<text x='{MapX(coordinates.X, coordinates.Y, radius) - minX}' y='{MapY(coordinates.Y, radius) - minY}' class='resource'>{map[coordinates]}</text>");
        }
        writer.WriteLine("</svg>");
    }
        
    public static double MapX(short x, short y, double radius)
    {
        double hexWidth = Math.Sqrt(3) * radius;
        return x * hexWidth + (y % 2) * (hexWidth / 2);
    }

    public static double MapY(short y, double radius)
    {
        double hexHeight = 3.0 / 2.0 * radius;
        return y * hexHeight;
    }

    public static string DrawHexagon(Coordinates coordinates, double radius, double minX, double minY)
    {
        var x = MapX(coordinates.X, coordinates.Y, radius) - minX;
        var y = MapY(coordinates.Y, radius) - minY;

        var path = new StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            var angle = 2 * Math.PI / 6 * (i + 0.5);
            var x_i = x + radius * Math.Cos(angle);
            var y_i = y + radius * Math.Sin(angle);

            if (i == 0)
            {
                path.Append($"M {x_i} {y_i} ");
            }
            else
            {
                path.Append($"L {x_i} {y_i} ");
            }
        }
        path.Append("Z");
        return path.ToString();
    }
}
