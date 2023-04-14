namespace SimulturnDomain.Entities;
public record Fight(IReadOnlyDictionary<string, Army> ArmyPerPlayer,
    IReadOnlyDictionary<string, Army> LossesPerPlayer)
{
    public Army Survivors(string player)
    {
        return ArmyPerPlayer[player] - LossesPerPlayer[player];
    }
}