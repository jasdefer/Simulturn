namespace Simulturn.Core.Model.Upgrade;
public record UpgradeStartMatter
{
    public short Cost { get; init; }
    public byte Duration { get; init; }
    public int Matter { get; set; }
}