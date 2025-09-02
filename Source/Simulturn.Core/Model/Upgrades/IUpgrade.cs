namespace Simulturn.Core.Model.Upgrades;
public interface IUpgrade
{
    short Cost { get; }
    byte Duration { get; }
    Upgrade Upgrade { get; }
}
