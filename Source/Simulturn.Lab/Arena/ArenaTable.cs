using System.Globalization;
using System.Text;
using Simulturn.Core.Analysis;

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
        foreach (var pairing in result.Rows.GroupBy(x => x.Bots))
        {
            PerBot<string> headings = [.. pairing.Key.Select(x => $"Wins of {x} (95 %)")];
            int width = Math.Max(WinsWidth, headings.Max(x => x.Length));

            table.AppendLine();
            table.AppendLine(CultureInfo.InvariantCulture, $"{string.Join(" against ", pairing.Key)}, seat of {pairing.Key[0]}");
            table.Append(CultureInfo.InvariantCulture, $"  {"Map",-12}{"Seat",4}{"Games",7}{"Draws",7}{"Turns",8}");
            table.AppendLine(string.Concat(headings.Select(x => "  " + x.PadRight(width))).TrimEnd());
            foreach (ArenaRow row in pairing)
            {
                table.Append(CultureInfo.InvariantCulture, $"  {row.Map,-12}{row.Seat,4}{row.Games,7}{row.Draws,7}{row.AverageTurns,8:F0}");
                table.AppendLine(string.Concat(row.Bots.Places.Select(bot => "  " + Wins(row, bot).PadRight(width))).TrimEnd());
            }
        }
        return table.ToString();
    }

    /// <summary>The width of the wins of a bot with the widest numbers.</summary>
    private const int WinsWidth = 31;

    private static string Wins(ArenaRow row, int bot)
    {
        WinRate rate = row.WinRates[bot];
        return string.Create(CultureInfo.InvariantCulture, $"{row.Wins[bot],5}  {rate.Rate,5:P0} ({rate.Low:P0} to {rate.High:P0})");
    }

    /// <summary>
    /// One line per game and bot of the run: the game, who won it (empty when nobody did), the
    /// bot of the line and every entry of its ledger at the end of the game.
    /// </summary>
    public static string RenderGamesCsv(ArenaResult result)
    {
        StringBuilder csv = new();
        csv.AppendLine($"FirstBot,SecondBot,Map,MapSeed,Seat,End,Turns,RemainingMatter,Winner,Bot,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (ArenaGameResult game in result.Games)
        {
            foreach (int bot in game.Key.Bots.Places)
            {
                csv.AppendLine(CultureInfo.InvariantCulture,
                    $"{string.Join(",", game.Key.Bots)},{game.Key.Map},{game.Key.MapSeed},{game.Key.Seat},{game.End},{game.Turns},{game.RemainingMatter},{game.WinnerName},{game.Key.Bots[bot]},{string.Join(",", LedgerEntries.All.Select(x => x.Value(game.Ledgers[bot])))}");
            }
        }
        return csv.ToString();
    }
}
