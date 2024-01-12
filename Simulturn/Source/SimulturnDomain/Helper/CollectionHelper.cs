namespace SimulturnDomain.Helper;
public static class CollectionHelper
{
    public static short Sum(this IEnumerable<short> collection)
    {
        var sum = 0;
        foreach (var item in collection)
        {
            sum+= item;
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
}