using SimulturnDomain.DataStructures;
using SimulturnDomain.Helper;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class IncomeHelper
{
    public static ushort GetIncome(HexMap<ushort> revenue, HexMap<Army> armies, Army requiredSpace, ImmutableArray<UpkeepLevel> upkeepLevels)
    {
        var sum = armies.Sum();
        var space = Convert.ToUInt16((sum * requiredSpace).Sum());
        var totalRevenue = revenue.Values.Sum();
        return GetIncome(totalRevenue, space, upkeepLevels);
    }

    public static ushort GetIncome(ushort totalRevenue, ushort space, IReadOnlyList<UpkeepLevel> upkeepLevels)
    {
        var upperBound = 0;
        var upkeep = 0d;
        for (int i = 0; i < upkeepLevels.Count; i++)
        {
            upperBound += upkeepLevels[i].SpaceDelta;
            if (space < upperBound)
            {
                return GetIncome(totalRevenue, upkeep);
            }
            upkeep = upkeepLevels[i].Upkeep;
        }
        return GetIncome(totalRevenue, upkeep);
    }

    public static ushort GetIncome(ushort totalRevenue, double upkeep)
    {
        var income = totalRevenue * (1 - upkeep);
        return Convert.ToUInt16(Math.Round(income));
    }
}
