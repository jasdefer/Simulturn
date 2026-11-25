using System.Globalization;
using System.Text;
using Simulturn.Core.Analysis;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Replay;

/// <summary>
/// Renders a replay: the ledgers of the game as a text table and the ledgers of every turn as CSV.
/// </summary>
public static class ReplayTable
{
    /// <summary>
    /// The ledgers of the game, the bots next to each other.
    /// </summary>
    public static string Render(PlayedGame replay)
    {
        StringBuilder table = new();
        PlayedTurn last = replay.Turns[^1];
        string end = replay.Game.WinnerName is { } winner ? $"{replay.Result.End} of {winner}" : $"{replay.Result.End}";
        table.AppendLine(CultureInfo.InvariantCulture, $"{replay.Game.Key}");
        table.AppendLine(CultureInfo.InvariantCulture, $"{end} after {replay.Result.Turns} turns, {last.RemainingMatter} matter left on the map");
        table.AppendLine();
        table.AppendLine(CultureInfo.InvariantCulture, $"{"",-24}{string.Concat(replay.Game.Key.Bots.Select(x => x.PadLeft(12)))}");
        foreach (LedgerSection section in LedgerEntries.Sections)
        {
            table.AppendLine(section.Heading);
            foreach (LedgerEntry entry in section.Entries)
            {
                table.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Name,-22}{string.Concat(last.Ledgers.Select(x => $"{entry.Value(x),12}"))}");
            }
        }
        return table.ToString();
    }

    /// <summary>
    /// One line per turn and bot with every entry of its ledger, summed up from the start of the game.
    /// </summary>
    public static string RenderCsv(PlayedGame replay)
    {
        StringBuilder csv = new();
        csv.AppendLine($"Turn,Bot,RemainingMatter,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (PlayedTurn turn in replay.Turns)
        {
            foreach (int bot in replay.Game.Key.Bots.Places)
            {
                csv.AppendLine(CultureInfo.InvariantCulture, $"{turn.Turn},{replay.Game.Key.Bots[bot]},{turn.RemainingMatter},{string.Join(",", LedgerEntries.All.Select(x => x.Value(turn.Ledgers[bot])))}");
            }
        }
        return csv.ToString();
    }
}
