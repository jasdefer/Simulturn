namespace Simulturn.Core.Model.Report;

/// <summary>
/// What happened to one player in a turn and concerns nobody else: no other player gets to see it.
/// </summary>
public sealed record PlayerActivityReport
{
    public static PlayerActivityReport Empty { get; } = new();

    /// <summary>
    /// The matter at the start of the turn.
    /// </summary>
    public int MatterBefore { get; init; }

    /// <summary>
    /// The matter at the end of the turn, after income and the cost of the new orders.
    /// </summary>
    public int MatterAfter { get; init; }

    /// <summary>
    /// The harvested matter per hexagon. Hexagons without income are missing.
    /// </summary>
    public ImmutableDictionary<Hexagon, IncomeReport> Incomes { get; init; } = ImmutableDictionary<Hexagon, IncomeReport>.Empty;

    /// <summary>
    /// The units that joined the army this turn, including trainings ordered in the same turn.
    /// </summary>
    public ImmutableDictionary<Hexagon, Army> CompletedTrainings { get; init; } = ImmutableDictionary<Hexagon, Army>.Empty;

    /// <summary>
    /// The buildings that were finished this turn, including constructions ordered in the same turn.
    /// </summary>
    public ImmutableDictionary<Hexagon, Compound> CompletedConstructions { get; init; } = ImmutableDictionary<Hexagon, Compound>.Empty;

    /// <summary>
    /// The researches that were completed this turn, keyed by the hexagon they ran on.
    /// </summary>
    public ImmutableDictionary<Hexagon, ResearchReport> CompletedResearches { get; init; } = ImmutableDictionary<Hexagon, ResearchReport>.Empty;

    /// <summary>
    /// The movements in the order they were resolved.
    /// </summary>
    public ImmutableArray<MovementReport> Movements { get; init; } = [];

    /// <summary>
    /// The pending constructions that stopped because their dots died in a fight.
    /// </summary>
    public ImmutableDictionary<Hexagon, Compound> CanceledConstructions { get; init; } = ImmutableDictionary<Hexagon, Compound>.Empty;

    /// <summary>
    /// The pending trainings that stopped because their building was destroyed. Their space is free again.
    /// </summary>
    public ImmutableDictionary<Hexagon, Army> CanceledTrainings { get; init; } = ImmutableDictionary<Hexagon, Army>.Empty;

    /// <summary>
    /// The pending researches that stopped because no dot was left on their hexagon. The matter is lost.
    /// </summary>
    public ImmutableDictionary<Hexagon, Upgrade> CanceledResearches { get; init; } = ImmutableDictionary<Hexagon, Upgrade>.Empty;
}
