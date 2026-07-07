using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Web.Board;

/// <summary>Human-readable descriptions of upgrades and their per-level effects.</summary>
public static class UpgradeInfo
{
    public static string DisplayName(Upgrade upgrade) => upgrade switch
    {
        Upgrade.DotUpgrade => "Dot combat training",
        Upgrade.UpgradeStartMatter => "Matter grant",
        _ => upgrade.ToString(),
    };

    public static string Describe(Upgrade upgrade) => upgrade switch
    {
        Upgrade.DotUpgrade =>
            "Increases the fight exponent of your dots (•), making groups of workers dramatically stronger in combat.",
        Upgrade.UpgradeStartMatter =>
            "Grants a lump sum of matter. (Defined in the engine but not applied by turn resolution yet.)",
        _ => "No description available.",
    };

    /// <summary>The concrete effect of one researchable level, e.g. "+20 dot fight exponent (×100)".</summary>
    public static string DescribeLevel(IUpgrade level) => level switch
    {
        DotUpgrade dotUpgrade => $"+{dotUpgrade.ExponentBonus} dot fight exponent (×100)",
        UpgradeStartMatter startMatter => $"+{startMatter.Matter} matter",
        _ => string.Empty,
    };
}
