namespace Simulturn.AI.Players;

/// <summary>
/// Tuning knobs of the <see cref="TacticianPlayer"/> on top of the commander options.
/// </summary>
public record TacticianPlayerOptions : CommanderPlayerOptions
{
    /// <summary>
    /// The minimum share of every fighter type in the army composition, so a wrong enemy
    /// estimate cannot produce a fully hard-countered army.
    /// </summary>
    public double CompositionHedge { get; init; } = 0.15;
}
