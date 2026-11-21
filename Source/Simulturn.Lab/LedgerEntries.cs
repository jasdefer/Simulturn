using Simulturn.Core.Analysis;

namespace Simulturn.Lab;

/// <summary>
/// One entry of a <see cref="MatterLedger"/> with the name it is shown under.
/// </summary>
public sealed record LedgerEntry(string Name, Func<MatterLedger, int> Value);

/// <summary>
/// The entries of a ledger that belong together, under one heading.
/// </summary>
public sealed record LedgerSection(string Heading, ImmutableArray<LedgerEntry> Entries);

/// <summary>
/// The entries of a <see cref="MatterLedger"/> in the order every study shows them. A table and
/// a file of the lab list the entries from here, so a new entry of the ledger needs one line.
/// </summary>
public static class LedgerEntries
{
    public static ImmutableArray<LedgerSection> Sections { get; } =
    [
        new("Came from",
        [
            new("StartMatter", x => x.StartMatter),
            new("StartStock", x => x.StartStock),
            new("Harvested", x => x.Harvested)
        ]),
        new("Spent on",
        [
            new("SpentOnDots", x => x.SpentOnDots),
            new("SpentOnFightingUnits", x => x.SpentOnFightingUnits),
            new("SpentOnBuildings", x => x.SpentOnBuildings),
            new("SpentOnResearch", x => x.SpentOnResearch)
        ]),
        new("Still there",
        [
            new("OnHand", x => x.OnHand),
            new("Dots", x => x.Dots),
            new("FightingUnits", x => x.FightingUnits),
            new("Buildings", x => x.Buildings),
            new("Research", x => x.Research),
            new("Pending", x => x.Pending)
        ]),
        new("Gone",
        [
            new("Fallen", x => x.Fallen),
            new("Destroyed", x => x.Destroyed),
            new("Canceled", x => x.Canceled)
        ]),
        new("Done to others",
        [
            new("Killed", x => x.Killed),
            new("KilledNeutral", x => x.KilledNeutral),
            new("Razed", x => x.Razed)
        ])
    ];

    /// <summary>
    /// All entries without their headings, in the order of the sections.
    /// </summary>
    public static ImmutableArray<LedgerEntry> All { get; } = [.. Sections.SelectMany(x => x.Entries)];
}
