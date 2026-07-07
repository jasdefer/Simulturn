using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Model.State;

public record PlayerState
{
    public int Matter { get; init; }
    public int UsedSpace { get; init; }
    public int AvailableSpace { get; init; }
    public required ImmutableDictionary<Hexagon, Army> Armies { get; init; }
    public required ImmutableDictionary<Hexagon, Compound> Compounds { get; init; }
    public required Trainings Trainings { get; init; }
    public required Constructions Constructions { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>> Researches { get; init; }
    public required ImmutableDictionary<Hexagon, Visibility> Visibilities { get; init; }
    /// <summary>
    /// The losses of the player in the current turn.
    /// </summary>
    public required ImmutableDictionary<Hexagon, Army> Losses { get; init; }

    /// <summary>
    /// A level of 0 means the upgrade is not researched and is equivalent to not having the element in the dictionary.
    /// </summary>
    public ImmutableDictionary<Upgrade, byte> UpgradeLevels { get; init; } = ImmutableDictionary<Upgrade, byte>.Empty;

    /// <summary>
    /// What the player remembers about each hexagon from the last turn it was at least partially visible.
    /// Hexagons that were never seen have no entry.
    /// </summary>
    public ImmutableDictionary<Hexagon, HexagonMemory> Memories { get; init; } = ImmutableDictionary<Hexagon, HexagonMemory>.Empty;

    /// <summary>
    /// The armies of other players this player fought against in the current turn, keyed by
    /// hexagon and opponent id. Fighting reveals the full composition of the participating
    /// armies (as they entered the fight, before losses).
    /// </summary>
    public ImmutableDictionary<Hexagon, ImmutableDictionary<string, Army>> RevealedArmies { get; init; } = ImmutableDictionary<Hexagon, ImmutableDictionary<string, Army>>.Empty;

    /// <summary>
    /// The number of researches of the given upgrade still in progress at the given turn.
    /// Only completed researches are reflected in <see cref="UpgradeLevels"/>.
    /// </summary>
    public int PendingResearchCount(Upgrade upgrade, ushort turn) =>
        Researches
            .Where(x => x.Key >= turn)
            .Sum(x => x.Value.Count(y => y.Value == upgrade));

    public Army ExponentBonusFromUpgrades(GameSettings gameSettings)
    {
        // TODO: Extend upgrade exponent bonuses to support all unit-specific upgrades.
        Army bonus = Army.Empty;
        if (UpgradeLevels.TryGetValue(Upgrade.DotUpgrade, out byte dotUpgradeLevel) && dotUpgradeLevel > 0)
        {
            // A level of 1 means the first upgrade, which is at index 0.
            bonus = bonus.AddUnit(Unit.Dot, ((DotUpgrade)gameSettings.Upgrades[Upgrade.DotUpgrade][dotUpgradeLevel - 1]).ExponentBonus);
        }
        return bonus;
    }
}
