using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.Interfaces;
public interface IGameRepository
{
    Task Add(Game game);
    Task EndTurn(string gameId, string player);
    Task<bool> HasPlayerEndedHisTurn(string gameId, string playerId);
    Task<bool> HaveAllPlayersEndedTheirTurn(string gameId);
    Task NewTurn(string gameId);
    Task<ushort> GetTurn(string gameId);
}
