namespace Simulturn.Core.Model;

public record HexagonSettings
{
    public static readonly HexagonSettings Empty = new()
    {
        Matter = 0,
        IsBuildable = true,
        MaxNumberOfUnitsGeneratingMatter = Army.Empty,
        PlayerInitialization = null
    };
    public HexagonSettings()
    {
    }

    public int Matter { get; init; } = 0;
    public Army MaxNumberOfUnitsGeneratingMatter { get; init; } = Army.Empty;
    public bool IsBuildable { get; init; } = true;
    public ImmutableArray<Upgrade> ResearchableUpgrades { get; init; } = ImmutableArray<Upgrade>.Empty;
    public (string StartingPlayerId, Army InitialArmy, Compound InitialCompound)? PlayerInitialization { get; init; } = null;
}