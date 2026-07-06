namespace Simulturn.AI.Players;

/// <summary>
/// Tuning knobs of the <see cref="SimulatorPlayer"/> on top of the commander options.
/// </summary>
public record SimulatorPlayerOptions : CommanderPlayerOptions
{
    /// <summary>
    /// How many turns each candidate strategy is rolled out on the believed state
    /// before the resulting position is evaluated.
    /// </summary>
    public int SimulationHorizon { get; init; } = 8;

    /// <summary>
    /// A candidate must beat the rule based decision by this evaluation margin (in matter
    /// units) under every simulated enemy behavior before it overrides the rules.
    /// </summary>
    public double OverrideMargin { get; init; } = 300;
}
