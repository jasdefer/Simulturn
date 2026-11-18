using System.Globalization;
using System.Text;

namespace Simulturn.Lab.Arena;

/// <summary>
/// Renders an arena result as a text table, one block per pairing.
/// </summary>
public static class ArenaTable
{
    public static string Render(ArenaResult result)
    {
        StringBuilder table = new();
        table.AppendLine(CultureInfo.InvariantCulture, $"Engine {result.EngineVersion}");
        foreach (var pairing in result.Rows.GroupBy(x => (x.Bot, x.Opponent)))
        {
            table.AppendLine();
            table.AppendLine(CultureInfo.InvariantCulture, $"{pairing.Key.Bot} against {pairing.Key.Opponent}");
            table.AppendLine(CultureInfo.InvariantCulture, $"  {"Map",-12}{"Seat",4}{"Games",7}{"Wins",7}{"Losses",8}{"Draws",7}{"Turns",8}  Win rate (95 %)");
            foreach (ArenaRow row in pairing)
            {
                table.AppendLine(CultureInfo.InvariantCulture,
                    $"  {row.Map,-12}{row.Seat,4}{row.Games,7}{row.Wins,7}{row.Losses,8}{row.Draws,7}{row.AverageTurns,8:F0}  {row.WinRate,4:P0} ({row.WinRateLow:P0} to {row.WinRateHigh:P0})");
            }
        }
        return table.ToString();
    }
}
