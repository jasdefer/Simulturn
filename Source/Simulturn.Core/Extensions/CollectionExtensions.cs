using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Extensions;

public static class CollectionExtensions
{
    public static Army Sum(this IEnumerable<Army> armies)
    {
        return armies.Aggregate(new Army(), (acc, army) => acc + army);
    }

    public static Compound Sum(this IEnumerable<Compound> armies)
    {
        return armies.Aggregate(new Compound(), (acc, compound) => acc + compound);
    }

    public static ImmutableDictionary<Upgrade, ImmutableArray<IUpgradeLevel>> ToDictionary(this IEnumerable<IUpgradeLevel> upgrades)
    {
        return upgrades
            .GroupBy(u => u.Upgrade)
            .ToImmutableDictionary(
                g => g.Key,
                g => g.ToImmutableArray()
            );
    }
}
