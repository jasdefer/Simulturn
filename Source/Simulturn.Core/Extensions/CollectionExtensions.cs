using Simulturn.Core.Model;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Frozen;

namespace Simulturn.Core.Extensions;

public static class CollectionExtensions
{
    extension(IEnumerable<Army> armies)
    {
        public Army Sum() => armies.Aggregate(new Army(), (acc, army) => acc + army);
    }

    extension(IEnumerable<Compound> compounds)
    {
        public Compound Sum() => compounds.Aggregate(new Compound(), (acc, compound) => acc + compound);
    }

    extension(IEnumerable<IUpgrade> upgrades)
    {
        public FrozenDictionary<Upgrade, ImmutableArray<IUpgrade>> ToDictionary() =>
            upgrades
                .GroupBy(u => u.Upgrade)
                .ToFrozenDictionary(
                    g => g.Key,
                    g => g.ToImmutableArray());
    }
}
