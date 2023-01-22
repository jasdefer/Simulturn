namespace SimulturnCore.Logic;
public static class TurnLogic
{
    public static void EndTurn()
    {
        GenerateIncome();
        TrainUnits();
        BuildStructure();
        MoveArmies();
        Fight();
    }

    private static void Fight()
    {
        throw new NotImplementedException();
    }

    private static void MoveArmies()
    {
        throw new NotImplementedException();
    }

    private static void BuildStructure()
    {
        throw new NotImplementedException();
    }

    private static void TrainUnits()
    {
        throw new NotImplementedException();
    }

    private static void GenerateIncome()
    {
        throw new NotImplementedException();
    }
}
