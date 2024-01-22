using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Helper;
public static class Printer
{
    public static void Print(string path, HexMap<ushort> map)
    {
        int minX = map.Keys.Min(x => x.X);
        int minY = map.Keys.Min(x => x.Y);
        int maxX = map.Keys.Max(x => x.X);
        int maxY = map.Keys.Max(x => x.Y);

        using var writer = new StreamWriter(path);
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                string content = "";
                // To Do adjust to hexagon map
                Coordinates? coordinates = map.Keys.SingleOrDefault(coordinates => coordinates.X == x && coordinates.Y == y);
                if(coordinates.HasValue)
                {
                    content = map[coordinates.Value].ToString();
                }
                writer.Write(content);
            }
            writer.Write(writer.NewLine);
        }
    }
}
