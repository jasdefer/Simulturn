namespace SimulturnApplication.Common.Interfaces;
public interface IPlayerRepository
{
    Task EndTurn(string gameId, string PlayerId);
}
