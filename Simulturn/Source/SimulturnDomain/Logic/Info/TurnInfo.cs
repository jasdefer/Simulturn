using SimulturnDomain.Entities;
using SimulturnDomain.Exceptions;

namespace SimulturnDomain.Logic.Info;
public static class TurnInfo
{
    public static Turn GetLastTurn(this IEnumerable<Turn> turns)
    {
        return turns
            .MaxBy(x => x.TurnNumber)
            ?? throw new InvalidOperationException("Cannot get the last turn from an empty collection.");
    }

    public static Turn GetLastTurn(this IEnumerable<Turn> turns, string playerId)
    {
        return turns
            .Where(x => x.PlayerId == playerId)
            .MaxBy(x => x.TurnNumber)
            ?? throw new PlayerIdNotFoundException(playerId);
    }
}