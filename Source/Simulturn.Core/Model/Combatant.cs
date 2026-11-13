namespace Simulturn.Core.Model;

/// <summary>
/// One side of a fight: an army and the upgrade levels of the player it belongs to.
/// The turn resolution passes the true levels, a forecast the levels its player knows of.
/// </summary>
public sealed record Combatant
{
    public required Army Army { get; init; }

    /// <summary>
    /// A level of 0 means the upgrade is not researched and is equivalent to not having the element in the dictionary.
    /// </summary>
    public ImmutableDictionary<Upgrade, byte> UpgradeLevels { get; init; } = ImmutableDictionary<Upgrade, byte>.Empty;
}
