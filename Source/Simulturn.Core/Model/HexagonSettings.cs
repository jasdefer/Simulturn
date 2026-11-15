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
    /// <summary>
    /// Whether a player may order a construction on this hexagon. The start buildings of the map
    /// do not depend on it.
    /// </summary>
    public bool IsBuildable { get; init; } = true;
    /// <summary>
    /// The upgrades a player may research on this hexagon. How many levels an upgrade has is
    /// defined once in <see cref="GameSettings.Upgrades"/>, not per hexagon.
    /// </summary>
    public ImmutableHashSet<Upgrade> ResearchableUpgrades { get; init; } = [];
    /// <summary>
    /// The neutral army that guards this hexagon from the start of the game, see <see cref="Neutral"/>.
    /// The settings are part of every player view, so every player knows where the guards
    /// start and what they consist of. A start hexagon holds no neutral army.
    /// </summary>
    public Army NeutralArmy { get; init; } = Army.Empty;
    public (string StartingPlayerId, Army InitialArmy, Compound InitialCompound)? PlayerInitialization { get; init; } = null;
}