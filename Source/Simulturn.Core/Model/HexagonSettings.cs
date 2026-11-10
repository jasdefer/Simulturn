namespace Simulturn.Core.Model;

public sealed record HexagonSettings
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
    /// <summary>
    /// The upgrades a player may research on this hexagon. How many levels an upgrade has is
    /// defined once in <see cref="GameSettings.Upgrades"/>, not per hexagon.
    /// </summary>
    public ImmutableHashSet<Upgrade> ResearchableUpgrades { get; init; } = [];
    public Army NeutralArmy { get; init; } = Army.Empty;
    public (string StartingPlayerId, Army InitialArmy, Compound InitialCompound)? PlayerInitialization { get; init; } = null;
}