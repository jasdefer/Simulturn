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

    /// <summary>
    /// The upgrades that acted on the army of every player, with their levels (see
    /// <see cref="Engine.UpgradeRules.ActingOn"/>). Every player of <see cref="Armies"/> has an
    /// entry, an empty one without such an upgrade. A fight lays the composition of the armies
    /// open and with it these upgrades; together with <see cref="Armies"/> they are all it takes
    /// to compute the fight again.
    /// </summary>
    public required ImmutableDictionary<string, ImmutableDictionary<Upgrade, byte>> UpgradeLevels { get; init; }
}
