using SimulturnCore.Model;

namespace SimulturnCore.Helper;
internal static class ArmyHelper
{
    internal static ushort GetTotalCost(this Army army, Army cost)
    {
        return (army * cost).Sum();
    }

    internal static ushort GetProvidedSpace(this Structure structure, Structure providedSpace)
    {
        return (structure * providedSpace).Sum();
    }

    internal static ushort GetRequiredSpace(this Army army, Army requiredSpace)
    {
        return (army * requiredSpace).Sum();
    }

    internal static bool EnoughMatter(this Army army,
        Army cost,
        ushort matter)
    {
        var enoughMatter = army.GetTotalCost(cost) <= matter;
        return enoughMatter;
    }
}
