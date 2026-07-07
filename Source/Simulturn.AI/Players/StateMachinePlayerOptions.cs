namespace Simulturn.AI.Players;

/// <summary>
/// Tuning knobs of the <see cref="StateMachinePlayer"/>. The defaults are calibrated
/// against the random baseline players via the arena.
/// </summary>
public record StateMachinePlayerOptions
{
    /// <summary>
    /// Stop training workers beyond this count, even if more hexagons could be saturated.
    /// </summary>
    public int MaxWorkers { get; init; } = 16;

    /// <summary>
    /// Expand to new income hexagons until this many own income bases exist.
    /// </summary>
    public int MaxIncomeBases { get; init; } = 2;

    /// <summary>
    /// The number of workers sent to build up an expansion.
    /// </summary>
    public int ExpansionWorkers { get; init; } = 3;

    /// <summary>
    /// The number of training buildings constructed per fighter type.
    /// </summary>
    public int TrainingBuildingsPerFighter { get; init; } = 2;

    /// <summary>
    /// Never attack with fewer fighting units than this.
    /// </summary>
    public int MinAttackArmy { get; init; } = 6;

    /// <summary>
    /// Attack only if the own simulated fight strength exceeds the estimated enemy strength by this factor.
    /// </summary>
    public double AttackMargin { get; init; } = 1.5;

    /// <summary>
    /// Send a new scout when the enemy intel is older than this many turns.
    /// </summary>
    public int ScoutRefreshTurns { get; init; } = 15;

    /// <summary>
    /// Only expand to hexagons with at least this much known or expected remaining matter.
    /// </summary>
    public int MinExpansionMatter { get; init; } = 300;
}
