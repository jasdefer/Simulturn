namespace Simulturn.Core.Model.Upgrades;

public readonly struct UpgradeStartMatter : IUpgrade
{
    public short Cost { get; init; }
    public byte Duration { get; init; }
    public int Matter { get; init; }
    public Upgrade Upgrade => Upgrade.UpgradeStartMatter;
}