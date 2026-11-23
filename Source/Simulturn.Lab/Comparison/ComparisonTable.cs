using System.Globalization;
using System.Text;
using Simulturn.Bots;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Comparison;

/// <summary>
/// Renders a comparison: one text block per bot, opponent and map type with the groups next to
/// each other, one CSV line per group and bot, and the games to look at.
/// </summary>
public static class ComparisonTable
{
    private const int LabelWidth = 24;

    private static readonly ImmutableArray<MatchEnd> _ends = [.. Enum.GetValues<MatchEnd>()];

    /// <summary>
    /// The average ledgers of the groups. Every group has a column per bot of the pairing; what
    /// belongs to the game and not to a bot stands under the first bot.
    /// </summary>
    public static string Render(IReadOnlyList<ComparisonGroup> groups)
    {
        StringBuilder table = new();
        foreach (var block in groups.GroupBy(x => (x.Bots, x.Map)))
        {
            List<ComparisonGroup> columns = [.. block];
            PerBot<string> bots = block.Key.Bots;
            int width = Math.Max(10, bots.Max(x => x.Length) + 2);
            string noBot = new(' ', (bots.Count - 1) * width);

            if (table.Length > 0)
            {
                table.AppendLine();
            }
            table.AppendLine(CultureInfo.InvariantCulture, $"{string.Join(" against ", bots)} on {block.Key.Map}");
            AppendLine(table, "", columns, x => $"{Heading(x)} ({x.Games.Length})".PadLeft(bots.Count * width));
            AppendLine(table, "", columns, x => string.Concat(bots.Select(bot => bot.PadLeft(width))));
            AppendLine(table, "  Turns", columns, x => Number(x.AverageTurns, width) + noBot);
            AppendLine(table, "  Left on the map", columns, x => Number(x.AverageRemainingMatter, width) + noBot);
            foreach (MatchEnd end in _ends.Where(end => columns.Any(x => x.EndedBy(end) > 0)))
            {
                AppendLine(table, $"  Ended by {end}", columns, x => Number(x.EndedBy(end), width) + noBot);
            }
            AppendLine(table, "  Typical game", columns, x => Game(x.Typical()).PadLeft(bots.Count * width));
            foreach (LedgerSection section in LedgerEntries.Sections)
            {
                table.AppendLine(section.Heading);
                foreach (LedgerEntry entry in section.Entries)
                {
                    AppendLine(table, $"  {entry.Name}", columns, x => string.Concat(bots.Places.Select(bot => Number(x.Average(bot, entry.Value), width))));
                }
            }
        }
        return table.ToString();
    }

    /// <summary>
    /// One line per group and bot: the group, how its games ended, and the average of every entry
    /// of the ledgers of the bot of the line. The winner is empty for the games nobody won.
    /// </summary>
    public static string RenderCsv(IReadOnlyList<ComparisonGroup> groups)
    {
        StringBuilder csv = new();
        csv.AppendLine($"FirstBot,SecondBot,Map,Winner,Games,{string.Join(",", _ends)},Turns,RemainingMatter,Bot,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (ComparisonGroup group in groups)
        {
            string game = string.Create(CultureInfo.InvariantCulture,
                $"{Prefix(group)},{string.Join(",", _ends.Select(group.EndedBy))},{group.AverageTurns:F1},{group.AverageRemainingMatter:F1}");
            foreach (int bot in group.Bots.Places)
            {
                string averages = string.Join(",", LedgerEntries.All.Select(x => group.Average(bot, x.Value).ToString("F1", CultureInfo.InvariantCulture)));
                csv.AppendLine(CultureInfo.InvariantCulture, $"{game},{group.Bots[bot]},{averages}");
            }
        }
        return csv.ToString();
    }

    /// <summary>
    /// The games to look at, named by map seed and seat so that <c>replay</c> can play them: one
    /// line per group for its typical game, and per bot and entry one for the game with the lowest
    /// and one for the game with the highest value. An entry that is the same in every game of a
    /// group has no lines.
    /// </summary>
    public static string RenderPicksCsv(IReadOnlyList<ComparisonGroup> groups)
    {
        StringBuilder csv = new();
        csv.AppendLine("FirstBot,SecondBot,Map,Winner,Games,Pick,Bot,Entry,Value,Average,MapSeed,Seat");
        foreach (ComparisonGroup group in groups)
        {
            ArenaGameKey typical = group.Typical().Key;
            csv.AppendLine(CultureInfo.InvariantCulture, $"{Prefix(group)},Typical,,,,,{typical.MapSeed},{typical.Seat}");
            foreach (int bot in group.Bots.Places)
            {
                AppendOutliers(csv, group, bot);
            }
        }
        return csv.ToString();
    }

    private static void AppendOutliers(StringBuilder csv, ComparisonGroup group, int bot)
    {
        foreach (LedgerEntry entry in LedgerEntries.All)
        {
            if (group.Outliers(bot, entry.Value) is not { } found)
            {
                continue;
            }
            foreach ((string pick, ArenaGameResult game) in new[] { ("Lowest", found.Lowest), ("Highest", found.Highest) })
            {
                csv.AppendLine(CultureInfo.InvariantCulture,
                    $"{Prefix(group)},{pick},{group.Bots[bot]},{entry.Name},{entry.Value(game.Ledgers[bot])},{group.Average(bot, entry.Value):F1},{game.Key.MapSeed},{game.Key.Seat}");
            }
        }
    }

    /// <summary>
    /// What names a group in a file: its bots, its map type, its winner and the number of its games.
    /// </summary>
    private static string Prefix(ComparisonGroup group)
    {
        return $"{string.Join(",", group.Bots)},{group.Map},{group.WinnerName},{group.Games.Length}";
    }

    private static string Game(ArenaGameResult game)
    {
        return string.Create(CultureInfo.InvariantCulture, $"seed {game.Key.MapSeed} seat {game.Key.Seat}");
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

    private static string Heading(ComparisonGroup group)
    {
        return group.WinnerName is { } winner ? $"{winner} won" : "No winner";
    }
}
