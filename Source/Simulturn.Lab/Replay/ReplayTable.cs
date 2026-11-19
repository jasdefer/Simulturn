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
    /// The entries of a ledger in the order they are shown. A null value is a heading.
    /// </summary>
    private static readonly ImmutableArray<(string Name, Func<MatterLedger, int>? Value)> _entries =
    [
        ("Came from", null),
        ("StartMatter", x => x.StartMatter),
        ("StartStock", x => x.StartStock),
        ("Harvested", x => x.Harvested),
        ("Spent on", null),
        ("SpentOnDots", x => x.SpentOnDots),
        ("SpentOnFightingUnits", x => x.SpentOnFightingUnits),
        ("SpentOnBuildings", x => x.SpentOnBuildings),
        ("SpentOnResearch", x => x.SpentOnResearch),
        ("Still there", null),
        ("OnHand", x => x.OnHand),
        ("Dots", x => x.Dots),
        ("FightingUnits", x => x.FightingUnits),
        ("Buildings", x => x.Buildings),
        ("Research", x => x.Research),
        ("Pending", x => x.Pending),
        ("Gone", null),
        ("Fallen", x => x.Fallen),
        ("Destroyed", x => x.Destroyed),
        ("Canceled", x => x.Canceled),
        ("Done to others", null),
        ("Killed", x => x.Killed),
        ("KilledNeutral", x => x.KilledNeutral),
        ("Razed", x => x.Razed)
    ];

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
        foreach ((string name, Func<MatterLedger, int>? value) in _entries)
        {
            if (value is null)
            {
                table.AppendLine(name);
                continue;
            }
            table.AppendLine(CultureInfo.InvariantCulture, $"  {name,-22}{value(last.Bot),12}{value(last.Opponent),12}");
        }
        return table.ToString();
    }

    /// <summary>
    /// One line per turn and bot with every entry of its ledger, summed up from the start of the game.
    /// </summary>
    public static string RenderCsv(ReplayResult replay)
    {
        StringBuilder csv = new();
        var values = _entries.Where(x => x.Value is not null).ToList();
        csv.AppendLine($"Turn,Bot,RemainingMatter,{string.Join(",", values.Select(x => x.Name))}");
        foreach (ReplayTurn turn in replay.Turns)
        {
            foreach ((string bot, MatterLedger ledger) in new[] { (replay.Key.Bot, turn.Bot), (replay.Key.Opponent, turn.Opponent) })
            {
                csv.AppendLine(CultureInfo.InvariantCulture, $"{turn.Turn},{bot},{turn.RemainingMatter},{string.Join(",", values.Select(x => x.Value!(ledger)))}");
            }
        }
        return csv.ToString();
    }
}
