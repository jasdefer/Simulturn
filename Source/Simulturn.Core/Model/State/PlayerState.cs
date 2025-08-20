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
    public required Constructinos Constructions { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>> Researches { get; init; }
    public required ImmutableDictionary<Hexagon, Visibility> Visibilities { get; init; }

    /// <summary>
    /// A level of 0 means the upgrade is not researched and is equivalent to not having the element in the dictionary.
    /// </summary>
    public ImmutableDictionary<Upgrade, byte> UpgradeLevels { get; init; } = ImmutableDictionary<Upgrade, byte>.Empty;

    public Army ExponentBonusFromUpgrades(GameSettings gameSettings)
    {
        Army bonus = Army.Empty;
        Army player1Upgrades = Army.Empty;
        if (UpgradeLevels.TryGetValue(Upgrade.DotUpgrade, out byte dotUpgradeLevel))
        {
            player1Upgrades = player1Upgrades.AddUnit(Unit.Dot, ((DotUpgrade)gameSettings.Upgrades[Upgrade.DotUpgrade][dotUpgradeLevel]).ExponentBonus);
        }
        return bonus;
    }
}