using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Helper;
public static class HexMapHelper
{
    public static Army Sum(this HexMap<Army> armyMap)
    {
        var sum = new Army();
        foreach (var army in armyMap.Values)
        {
            sum += army;
        }
        return sum;
    }
}
