using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using System.Globalization;
using System.Text;

namespace Simulturn.Core.Helper;
public static class Printer
{
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
            var points = GetHexagonCorners(hexagon)
                .ToArray();
            sb.AppendLine($"<polygon class='hex' points='{string.Join(" ", points.Select(p => $"{p.x} {p.y}"))}' fill='{hexagonColor}' stroke='{_hexagonStrokeColor}' stroke-width='{_hexagonStrokeWidth}'></polygon>");
            sb.AppendLine(GetTextElement(info, points));
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string GetTextElement(List<string> info, (double x, double y)[] points)
    {

        double cx = points.Sum(p => p.x) / points.Length;
        double cy = points.Sum(p => p.y) / points.Length;

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

    private static IEnumerable<(double x, double y)> GetHexagonCorners(Hexagon hexagon)
    {
        double cx = _squareRootOfThree * _hexagonSize * (hexagon.X + hexagon.Z / 2.0);
        double cy = 1.5 * _hexagonSize * hexagon.Z;

        for (int i = 0; i < 6; i++)
        {
            double angle = Math.PI / 180.0 * (60 * i - 30);
            yield return (cx + _hexagonSize * Math.Cos(angle), cy + _hexagonSize * Math.Sin(angle));
        }
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
