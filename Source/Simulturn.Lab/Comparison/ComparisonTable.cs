using System.Globalization;
using System.Text;
using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Comparison;

/// <summary>
/// Renders a comparison: one text block per bot, opponent and map type with the groups next to
/// each other, and one CSV line per group and bot.
/// </summary>
public static class ComparisonTable
{
    private const int LabelWidth = 24;

    private static readonly ImmutableArray<MatchEnd> _ends = [.. Enum.GetValues<MatchEnd>()];

    /// <summary>
    /// The average ledgers of the groups. Every group has a column for the bot and one for its
    /// opponent; what belongs to the game and not to a bot stands under the bot.
    /// </summary>
    public static string Render(IReadOnlyList<ComparisonGroup> groups)
    {
        StringBuilder table = new();
        foreach (var block in groups.GroupBy(x => (x.Bot, x.Opponent, x.Map)))
        {
            List<ComparisonGroup> columns = [.. block];
            int width = Math.Max(10, Math.Max(block.Key.Bot.Length, block.Key.Opponent.Length) + 2);

            if (table.Length > 0)
            {
                table.AppendLine();
            }
            table.AppendLine(CultureInfo.InvariantCulture, $"{block.Key.Bot} against {block.Key.Opponent} on {block.Key.Map}");
            AppendLine(table, "", columns, x => $"{Heading(x.Outcome)} ({x.Games.Length})".PadLeft(2 * width));
            AppendLine(table, "", columns, x => block.Key.Bot.PadLeft(width) + block.Key.Opponent.PadLeft(width));
            AppendLine(table, "  Turns", columns, x => Number(x.AverageTurns, width) + new string(' ', width));
            AppendLine(table, "  Left on the map", columns, x => Number(x.AverageRemainingMatter, width) + new string(' ', width));
            foreach (MatchEnd end in _ends.Where(end => columns.Any(x => x.EndedBy(end) > 0)))
            {
                AppendLine(table, $"  Ended by {end}", columns, x => Number(x.EndedBy(end), width) + new string(' ', width));
            }
            foreach (LedgerSection section in LedgerEntries.Sections)
            {
                table.AppendLine(section.Heading);
                foreach (LedgerEntry entry in section.Entries)
                {
                    AppendLine(table, $"  {entry.Name}", columns, x => Number(x.BotAverage(entry.Value), width) + Number(x.OpponentAverage(entry.Value), width));
                }
            }
        }
        return table.ToString();
    }

    /// <summary>
    /// One line per group and bot: the group, how its games ended, and the average of every entry
    /// of the ledgers of the bot of the line. The outcome is the one of the first bot of the pairing.
    /// </summary>
    public static string RenderCsv(IReadOnlyList<ComparisonGroup> groups)
    {
        StringBuilder csv = new();
        csv.AppendLine($"Bot,Opponent,Map,Outcome,Games,{string.Join(",", _ends)},Turns,RemainingMatter,Player,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (ComparisonGroup group in groups)
        {
            string game = string.Create(CultureInfo.InvariantCulture,
                $"{group.Bot},{group.Opponent},{group.Map},{group.Outcome},{group.Games.Length},{string.Join(",", _ends.Select(group.EndedBy))},{group.AverageTurns:F1},{group.AverageRemainingMatter:F1}");
            csv.AppendLine(CultureInfo.InvariantCulture, $"{game},{group.Bot},{Averages(group.BotAverage)}");
            csv.AppendLine(CultureInfo.InvariantCulture, $"{game},{group.Opponent},{Averages(group.OpponentAverage)}");
        }
        return csv.ToString();
    }

    private static string Averages(Func<Func<MatterLedger, int>, double> average)
    {
        return string.Join(",", LedgerEntries.All.Select(x => average(x.Value).ToString("F1", CultureInfo.InvariantCulture)));
    }

    private static void AppendLine(StringBuilder table, string label, List<ComparisonGroup> columns, Func<ComparisonGroup, string> cell)
    {
        table.Append(label.PadRight(LabelWidth));
        foreach (ComparisonGroup column in columns)
        {
            table.Append(cell(column));
        }
        table.AppendLine();
    }

    private static string Number(double value, int width)
    {
        return value.ToString("F0", CultureInfo.InvariantCulture).PadLeft(width);
    }

    private static string Heading(ArenaOutcome outcome)
    {
        return outcome switch
        {
            ArenaOutcome.Win => "Won",
            ArenaOutcome.Loss => "Lost",
            ArenaOutcome.Draw => "No winner",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome.")
        };
    }
}
