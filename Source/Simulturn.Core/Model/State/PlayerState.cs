namespace Simulturn.Core.Model.State;

public sealed record PlayerState
{
    public int Matter { get; init; }
    public int UsedSpace { get; init; }
    public int AvailableSpace { get; init; }
    public required ImmutableDictionary<Hexagon, Army> Armies { get; init; }
    public required ImmutableDictionary<Hexagon, Compound> Compounds { get; init; }
    /// <summary>
    /// The pending trainings, keyed by completion turn and hexagon. Holds no empty entries.
    /// </summary>
    public required ImmutableDictionary<TurnHexagon, Army> Trainings { get; init; }
    /// <summary>
    /// The pending constructions, keyed by completion turn and hexagon. Holds no empty entries.
    /// </summary>
    public required ImmutableDictionary<TurnHexagon, Compound> Constructions { get; init; }
    /// <summary>
    /// The pending researches, keyed by hexagon. A hexagon hosts at most one research, and an
    /// upgrade is researched on at most one hexagon at a time (work item research-cost-and-duration).
    /// </summary>
    public required ImmutableDictionary<Hexagon, Research> Researches { get; init; }
    /// <summary>
    /// A level of 0 means the upgrade is not researched and is equivalent to not having the element in the dictionary.
    /// </summary>
    public ImmutableDictionary<Upgrade, byte> UpgradeLevels { get; init; } = ImmutableDictionary<Upgrade, byte>.Empty;

    /// <summary>
    /// The upgrades with a pending research. The next level of such an upgrade can only be
    /// started once the pending research has completed.
    /// </summary>
    public ImmutableHashSet<Upgrade> UpgradesInResearch()
    {
        return Researches.Values.Select(research => research.Upgrade).ToImmutableHashSet();
    }
}
