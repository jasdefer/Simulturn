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

    public static HexMap<Structure> Subtract(this HexMap<Structure> current, HexMap<Structure> structure)
    {
        Dictionary<Coordinates, Structure> result = current.ToDictionary();
        foreach (var coordinates in structure.Keys)
        {
            result[coordinates] -= structure[coordinates];
        }
        return new HexMap<Structure>(result);
    }
}
