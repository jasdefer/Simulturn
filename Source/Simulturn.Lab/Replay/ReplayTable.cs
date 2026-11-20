using System.Globalization;
using System.Text;
using Simulturn.Core.Analysis;

namespace Simulturn.Lab.Replay;

/// <summary>
/// Renders a replay: the ledgers of the game as a text table and the ledgers of every turn as CSV.
/// </summary>
public static class ReplayTable
{
    /// <summary>
    /// The ledgers of the game, the bot next to its opponent.
    /// </summary>
    public static string Render(ReplayResult replay)
    {
        StringBuilder table = new();
        ReplayTurn last = replay.Turns[^1];
        string end = replay.Result.WinnerId is null ? $"{replay.Result.End}" : $"{replay.Result.End} of {replay.Result.WinnerId}";
        table.AppendLine(CultureInfo.InvariantCulture, $"{replay.Key.Bot} on seat {replay.Key.Seat} against {replay.Key.Opponent} on map {replay.Key.Map} with seed {replay.Key.MapSeed}");
        table.AppendLine(CultureInfo.InvariantCulture, $"{end} after {replay.Result.Turns} turns, {last.RemainingMatter} matter left on the map");
        table.AppendLine();
        table.AppendLine(CultureInfo.InvariantCulture, $"{"",-24}{replay.Key.Bot,12}{replay.Key.Opponent,12}");
        foreach (LedgerSection section in LedgerEntries.Sections)
        {
            table.AppendLine(section.Heading);
            foreach (LedgerEntry entry in section.Entries)
            {
                table.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Name,-22}{entry.Value(last.Bot),12}{entry.Value(last.Opponent),12}");
            }
        }
        return table.ToString();
    }

    /// <summary>
    /// One line per turn and bot with every entry of its ledger, summed up from the start of the game.
    /// </summary>
    public static string RenderCsv(ReplayResult replay)
    {
        StringBuilder csv = new();
        csv.AppendLine($"Turn,Bot,RemainingMatter,{string.Join(",", LedgerEntries.All.Select(x => x.Name))}");
        foreach (ReplayTurn turn in replay.Turns)
        {
            foreach ((string bot, MatterLedger ledger) in new[] { (replay.Key.Bot, turn.Bot), (replay.Key.Opponent, turn.Opponent) })
            {
                csv.AppendLine(CultureInfo.InvariantCulture, $"{turn.Turn},{bot},{turn.RemainingMatter},{string.Join(",", LedgerEntries.All.Select(x => x.Value(ledger)))}");
            }
        }
        return csv.ToString();
    }
}
