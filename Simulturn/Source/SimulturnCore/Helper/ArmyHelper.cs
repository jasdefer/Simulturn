using SimulturnCore.Model;

namespace SimulturnCore.Helper;
public static class ArmyHelper
{
    internal static short GetTotalCost(this Army army, Army cost)
    {
        return (army * cost).Sum();
    }

    internal static short GetProvidedSpace(this Structure structure, Structure providedSpace)
    {
        return (structure * providedSpace).Sum();
    }

    internal static short GetRequiredSpace(this Army army, Army requiredSpace)
    {
        return (army * requiredSpace).Sum();
    }

    internal static bool EnoughMatter(this Army army,
        Army cost,
        short matter)
    {
        var enoughMatter = army.GetTotalCost(cost) <= matter;
        return enoughMatter;
    }

    public static Army ArmyWithRequiredStructures(this Army army, Army requiredStructureIds, Structure structures)
    {
        return army * (requiredStructureIds * structures);
    }
}
