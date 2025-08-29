using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using System.Globalization;
using System.Text;

namespace Simulturn.Core.Helper;
public static class Printer
{
    private static readonly Random _random = new Random(1);
    private const int _hexagonSize = 100;
    private const string _hexagonFillColor = "#EEEEEE";
    private const string _hexagonStrokeColor = "#424242";
    private static readonly ImmutableArray<string> _playerColors = ["#fbc02d", "#2d67fb", "#F44336", "#4CAF50"];
    private const int _hexagonStrokeWidth = 2;
    private static readonly double _squareRootOfThree = Math.Sqrt(3);

    public static List<string> GetFullInfo(GameState gameState, Hexagon hexagon)
    {
        List<string> info = [];
        var remainingMatter = gameState.RemainingMatter[hexagon];
        if (remainingMatter > 0)
        {
            info.Add($"Remaining Matter: {remainingMatter}");
        }
        foreach ((string playerId, var playerState) in gameState.PlayerStates)
        {
            if (playerState.Armies.TryGetValue(hexagon, out var army) && !army.IsEmpty)
            {
                info.Add($"{playerId}: {army.ToCompactString()}");
            }
            if (playerState.Compounds.TryGetValue(hexagon, out var compound) && !compound.IsEmpty)
            {
                info.Add($"{playerId}: {compound.ToCompactString()}");
            }
            foreach ((ushort turn, var trainings) in playerState.Trainings.Where(x => x.Key >= gameState.Turn))
            {
                if (trainings.TryGetValue(hexagon, out var training) && !training.IsEmpty)
                {
                    info.Add($"Turn {turn} Training: {training.ToCompactString()}");
                }
            }
        }
        return info;
    }

    public static string PrintState(GameState gameState, Func<GameState, Hexagon, List<string>> getInfo)
    {
        StringBuilder sb = new();
        (double minX, double minY, double maxX, double maxY) = GetBoundingBox(gameState.Hexagons);
        sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{minX} {minY} {maxX - minX} {maxY - minY}'>");
        sb.AppendLine("<defs>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    <![CDATA[.lbl{font:8px sans-serif; text-anchor:middle; dominant-baseline:middle; fill:#111}]]>");
        sb.AppendLine("  </style>");
        sb.AppendLine("</defs>");

        int playerColorId = 0;
        ImmutableDictionary<string, string> playerColors = gameState.PlayerIds
            .Order()
            .ToImmutableDictionary(
                kvp => kvp,
                kvp => _playerColors[playerColorId++]);

        foreach (var hexagon in gameState.Hexagons)
        {
            List<string> info = getInfo(gameState, hexagon);

            string[] hexagonPlayers = gameState.PlayerStates
                .Where(x => x.Value.Armies.ContainsKey(hexagon) || x.Value.Compounds.ContainsKey(hexagon))
                .Select(x => x.Key)
                .ToArray();

            string hexagonColor = hexagonPlayers.Length == 1 ? playerColors[hexagonPlayers[0]] : _hexagonFillColor;

            // Generate hand-drawn path
            double cx = _squareRootOfThree * _hexagonSize * (hexagon.X + hexagon.Z / 2.0);
            double cy = 1.5 * _hexagonSize * hexagon.Z;

            var idealCorners = new (double x, double y)[6];
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 180.0 * (60 * i - 30);
                idealCorners[i] = (cx + _hexagonSize * Math.Cos(angle), cy + _hexagonSize * Math.Sin(angle));
            }

            double jitter = _hexagonSize * 0.04;
            var randomizedCorners = new (double x, double y)[6];
            for (int i = 0; i < 6; i++)
            {
                randomizedCorners[i] = (idealCorners[i].x + (_random.NextDouble() * 2 - 1) * jitter,
                                        idealCorners[i].y + (_random.NextDouble() * 2 - 1) * jitter);
            }

            StringBuilder path = new();
            path.Append(CultureInfo.InvariantCulture, $"M {randomizedCorners[0].x:F2} {randomizedCorners[0].y:F2} ");

            for (int i = 0; i < 6; i++)
            {
                var p1 = randomizedCorners[i];
                var p2 = randomizedCorners[(i + 1) % 6];

                double dx = p2.x - p1.x;
                double dy = p2.y - p1.y;

                double controlJitter = _hexagonSize * 0.1;

                var cp1 = (x: p1.x + dx * 0.25 + (_random.NextDouble() * 2 - 1) * controlJitter,
                           y: p1.y + dy * 0.25 + (_random.NextDouble() * 2 - 1) * controlJitter);

                var cp2 = (x: p1.x + dx * 0.75 + (_random.NextDouble() * 2 - 1) * controlJitter,
                           y: p1.y + dy * 0.75 + (_random.NextDouble() * 2 - 1) * controlJitter);

                path.Append(CultureInfo.InvariantCulture, $"C {cp1.x:F2} {cp1.y:F2}, {cp2.x:F2} {cp2.y:F2}, {p2.x:F2} {p2.y:F2} ");
            }
            path.Append("Z");
            string pathData = path.ToString();

            sb.AppendLine(CultureInfo.InvariantCulture, $"<path class='hex' d='{pathData}' fill='{hexagonColor}' stroke='{_hexagonStrokeColor}' stroke-width='{_hexagonStrokeWidth}'></path>");
            sb.AppendLine(GetTextElement(info, (cx, cy)));
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string GetTextElement(List<string> info, (double x, double y) center)
    {
        double cx = center.x;
        double cy = center.y;

        // 3) Available vertical space = s (rectangle height), minus tiny padding
        double padding = Math.Max(1, _hexagonStrokeWidth);
        double availableHeight = _hexagonSize - 2 * padding;
        double lineGapEm = 1.2;

        // Font-size to fit vertically
        double fs = availableHeight / (info.Count * lineGapEm);
        if (double.IsNaN(fs) || fs <= 0)
        {
            fs = 8;
        }

        fs = Math.Clamp(fs, 6, 14);

        StringBuilder sb = new();
        sb.Append(CultureInfo.InvariantCulture,
            $"<text class='lbl' x='{cx:F2}' font-size='{fs:F2}'>");

        for (int i = 0; i < info.Count; i++)
        {
            double y = cy + (i - (info.Count - 1) / 2.0) * (fs * lineGapEm);
            sb.Append(CultureInfo.InvariantCulture,
                $"<tspan x='{cx:F2}' y='{y:F2}'>{info[i]}</tspan>");
        }

        sb.Append("</text>");
        return sb.ToString();
    }

    private static (double minX, double minY, double maxX, double maxY) GetBoundingBox(IEnumerable<Hexagon> hexagons)
    {
        var centers = hexagons.Select(hexagon =>
        {
            double cx = _squareRootOfThree * _hexagonSize * (hexagon.X + hexagon.Z / 2.0);
            double cy = 1.5 * _hexagonSize * hexagon.Z;
            return (cx, cy);
        }).ToArray();

        double halfW = _squareRootOfThree * 0.5 * _hexagonSize + _hexagonStrokeWidth / 2;
        double halfH = _hexagonSize + _hexagonStrokeWidth / 2;

        double minX = centers.Min(c => c.cx) - halfW;
        double minY = centers.Min(c => c.cy) - halfH;
        double maxX = centers.Max(c => c.cx) + halfW;
        double maxY = centers.Max(c => c.cy) + halfH;

        return (minX, minY, maxX, maxY);
    }
}
