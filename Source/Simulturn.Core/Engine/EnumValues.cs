using Simulturn.Core.Model;

namespace Simulturn.Core.Engine;

internal static class EnumValues
{
    public static readonly ImmutableArray<Unit> Units = [.. Enum.GetValues<Unit>()];
    public static readonly ImmutableArray<Building> Buildings = [.. Enum.GetValues<Building>()];
    public static readonly ImmutableArray<Upgrade> Upgrades = [.. Enum.GetValues<Upgrade>()];
}
