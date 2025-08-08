namespace Simulturn.Core.Model.Upgrades;
public readonly struct DotUpgrade : IUpgrade
{
    public short Cost { get; init; }
    public byte Duration { get; init; }
    public short ExponentBonus { get; init; }
    public Upgrade Upgrade => Upgrade.DotUpgrade;
}