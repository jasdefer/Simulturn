using SimulturnDomain.Entities;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Common.Interfaces;
public interface IGameRepository
{
    Task<bool> AllPlayersEndedTheirTurn(string gameId);
    Task<Game> Get(string gameId);
    Task Update(Game game);
}
