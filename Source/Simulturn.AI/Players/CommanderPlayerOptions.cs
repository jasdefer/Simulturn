namespace Simulturn.AI.Players;

/// <summary>
/// Tuning knobs of the <see cref="CommanderPlayer"/>. Runs a larger economy than the
/// state machine player and adds defense, sweeping and late game aggression.
/// </summary>
public record CommanderPlayerOptions : StateMachinePlayerOptions
{
    public CommanderPlayerOptions()
    {
        // Matter on the map is finite: more workers only extract it faster while costing
        // matter and supply themselves, so the worker count stays lean.
        MaxWorkers = 16;
        MaxIncomeBases = 3;
        TrainingBuildingsPerFighter = 2;
        MinAttackArmy = 10; // attack in one decisive wave instead of trickling
    }

    /// <summary>
    /// Enemy units within this distance of an own building trigger the defend mode.
    /// </summary>
    public int DefenseRadius { get; init; } = 2;

    /// <summary>
    /// From this turn on the attack margin is dropped to 1, so long games still get decided.
    /// </summary>
    public ushort LateGameTurn { get; init; } = 120;

    /// <summary>
    /// Assumed enemy army growth per turn since the last observation of enemy units.
    /// </summary>
    public double EnemyGrowthPerTurn { get; init; } = 0.15;

    /// <summary>
    /// Upper bound of the assumed enemy growth, so stale intel does not make the enemy
    /// look invincible and paralyze the attack decision.
    /// </summary>
    public int MaxAssumedEnemyGrowth { get; init; } = 8;

    /// <summary>
    /// Keep at least this much free plus pending supply space, so unit production is
    /// never blocked by missing space.
    /// </summary>
    public int SpaceBuffer { get; init; } = 12;

    /// <summary>
    /// Research upgrades only when at least this much matter is banked, so research
    /// never starves the army production.
    /// </summary>
    public int ResearchMatterSurplus { get; init; } = 500;
}
