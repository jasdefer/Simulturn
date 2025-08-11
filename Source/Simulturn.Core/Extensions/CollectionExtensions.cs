using Simulturn.Core.Model;

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
}
