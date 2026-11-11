namespace Simulturn.Core.Model.Report;

/// <summary>
/// One fight on one hexagon, keyed by player id.
/// </summary>
public sealed record FightReport
{
    /// <summary>
    /// The armies as they entered the fight, before losses.
    /// </summary>
    public required ImmutableDictionary<string, Army> Armies { get; init; }

    /// <summary>
    /// The losses of the fight. Every player of <see cref="Armies"/> has an entry, an empty one without losses.
    /// </summary>
    public required ImmutableDictionary<string, Army> Losses { get; init; }
}
