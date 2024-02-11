using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Helper;
public static class CollectionHelper
{
    public static short Sum(this IEnumerable<short> collection)
    {
        var sum = 0;
        foreach (var item in collection)
        {
            sum += item;
        }
        return Convert.ToInt16(sum);
    }

    public static ushort Sum(this IEnumerable<ushort> collection)
    {
        var sum = 0;
        foreach (var item in collection)
        {
            sum += item;
        }
        return Convert.ToUInt16(sum);
    }

    public static Structure Sum(IEnumerable<Structure> structures)
    {
        Structure sum = new();
        foreach (var structure in structures)
        {
            sum += structure;
        }
        return sum;
    }

    public static Army Sum(IEnumerable<Army> armies)
    {
        Army sum = new();
        foreach (var army in armies)
        {
            sum += army;
        }
        return sum;
    }
}