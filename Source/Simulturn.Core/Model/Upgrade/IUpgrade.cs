namespace Simulturn.Core.Model.Upgrade;
public interface IUpgrade
{
    short Cost { get; }
    byte Duration { get; }
}
