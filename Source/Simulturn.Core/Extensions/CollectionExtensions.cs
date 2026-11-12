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

    extension(IReadOnlyDictionary<TurnHexagon, Army> trainings)
    {
        /// <summary>
        /// The pending trainings on the hexagon, all completion turns added up.
        /// </summary>
        public Army On(Hexagon hexagon) => trainings.Where(x => x.Key.Hexagon == hexagon).Select(x => x.Value).Sum();
    }

    extension(IReadOnlyDictionary<TurnHexagon, Compound> constructions)
    {
        /// <summary>
        /// The pending constructions on the hexagon, all completion turns added up.
        /// </summary>
        public Compound On(Hexagon hexagon) => constructions.Where(x => x.Key.Hexagon == hexagon).Select(x => x.Value).Sum();
    }

    extension(IEnumerable<IUpgradeLevel> upgrades)
    {
        public FrozenDictionary<Upgrade, ImmutableArray<IUpgradeLevel>> ToDictionary() =>
            upgrades
                .GroupBy(u => u.Upgrade)
                .ToFrozenDictionary(
                    g => g.Key,
                    g => g.ToImmutableArray());
    }
}
