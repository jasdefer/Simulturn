using SimulturnCore.Helper;
using SimulturnCore.Model;

namespace SimulturnCore.Logic;
public class PlanLogic
{
    public static bool CanTrain(Army army, Army cost, ushort matter, ushort remainingSpace)
    {
        var enoughMatter = army.EnoughMatter(cost, matter);
        if (!enoughMatter)
        {
            return false;
        }
        if (remainingSpace <= 0)
        {
            return false;
        }
        return true;
    }
}
