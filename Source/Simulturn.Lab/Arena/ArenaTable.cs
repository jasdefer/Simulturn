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

    /// <summary>
    /// One line per game and bot of the run: the game, how it ended for the bot of the line and
    /// every entry of its ledger at the end of the game.
    /// </summary>
    public static string RenderGamesCsv(ArenaResult result)
    {
        StringBuilder csv = new();
        csv.AppendLine($"Bot,Opponent,Map,MapSeed,Seat,End,Turns,RemainingMatter,Player,Outcome,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (ArenaGameResult game in result.Games)
        {
            ArenaOutcome opponentOutcome = game.Outcome switch
            {
                ArenaOutcome.Win => ArenaOutcome.Loss,
                ArenaOutcome.Loss => ArenaOutcome.Win,
                _ => ArenaOutcome.Draw
            };
            foreach ((string player, ArenaOutcome outcome, MatterLedger ledger) in new[] { (game.Key.Bot, game.Outcome, game.Bot), (game.Key.Opponent, opponentOutcome, game.Opponent) })
            {
                csv.AppendLine(CultureInfo.InvariantCulture,
                    $"{game.Key.Bot},{game.Key.Opponent},{game.Key.Map},{game.Key.MapSeed},{game.Key.Seat},{game.End},{game.Turns},{game.RemainingMatter},{player},{outcome},{string.Join(",", LedgerEntries.All.Select(x => x.Value(ledger)))}");
            }
        }
        return csv.ToString();
    }
}
