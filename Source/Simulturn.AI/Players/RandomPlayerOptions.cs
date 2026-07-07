namespace Simulturn.AI.Players;

/// <summary>
/// Controls how often the <see cref="RandomPlayer"/> acts and where its armies tend to move.
/// The movement weights bias the random destination choice; they do not make the player smart.
/// </summary>
public record RandomPlayerOptions
{
    /// <summary>
    /// The probability per own hexagon with idle dots to start a construction.
    /// </summary>
    public double ConstructionProbability { get; init; } = 0.5;

    /// <summary>
    /// The probability per own hexagon with an idle training building to train units.
    /// </summary>
    public double TrainingProbability { get; init; } = 0.7;

    /// <summary>
    /// The probability per turn to research an upgrade.
    /// </summary>
    public double ResearchProbability { get; init; } = 0.1;

    /// <summary>
    /// The probability per own hexagon with a movable army to move a part of it.
    /// </summary>
    public double MovementProbability { get; init; } = 0.3;

    /// <summary>
    /// Additional weight for destinations that get closer to the nearest known enemy building.
    /// </summary>
    public double AttackWeight { get; init; } = 1;

    /// <summary>
    /// Additional weight for destinations that get closer to the nearest own building.
    /// </summary>
    public double DefendWeight { get; init; } = 1;

    /// <summary>
    /// Additional weight for destinations that were never seen or still hold harvestable matter.
    /// </summary>
    public double ExpandWeight { get; init; } = 1;

    /// <summary>
    /// No movement bias at all: every destination is equally likely. The pure chaos baseline.
    /// </summary>
    public static RandomPlayerOptions Uniform => new()
    {
        AttackWeight = 0,
        DefendWeight = 0,
        ExpandWeight = 0
    };

    public static RandomPlayerOptions Aggressive => new()
    {
        MovementProbability = 0.5,
        AttackWeight = 5,
        DefendWeight = 0.5,
        ExpandWeight = 1
    };

    public static RandomPlayerOptions Defensive => new()
    {
        AttackWeight = 0,
        DefendWeight = 5,
        ExpandWeight = 0.5
    };

    public static RandomPlayerOptions Expansive => new()
    {
        MovementProbability = 0.4,
        AttackWeight = 0.5,
        DefendWeight = 0.5,
        ExpandWeight = 5
    };
}
