namespace Simulturn.Core.Model.Upgrades;

public interface IUpgradeLevel
{
    short Cost { get; }
    byte Duration { get; }
    Upgrade Upgrade { get; }
}
