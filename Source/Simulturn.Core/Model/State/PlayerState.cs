namespace Simulturn.Core.Model.State;

public record PlayerState
{
    public int Matter { get; init; }
    public int UsedSpace { get; init; }
    public int AvailableSpace { get; init; }
    public ImmutableDictionary<Upgrade, byte> UpgradeLevels { get; init; } = ImmutableDictionary<Upgrade, byte>.Empty;

    public Army ExponentBonusFromUpgrades(GameSettings gameSettings)
    {
        Army bonus = Army.Empty;
        Army player1Upgrades = Army.Empty;
        if (UpgradeLevels.TryGetValue(Upgrade.DotUpgrade, out byte dotUpgradeLevel))
        {
            player1Upgrades = player1Upgrades.AddUnit(Unit.Dot, gameSettings.DotUpgrades[dotUpgradeLevel].ExponentBonus);
        }
        return bonus;
    }
}