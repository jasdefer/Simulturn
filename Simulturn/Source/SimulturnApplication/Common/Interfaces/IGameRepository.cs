using SimulturnDomain.Entities;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Common.Interfaces;
public interface IGameRepository
{
    Task<bool> AllPlayersEndedTheirTurn(string gameId);
    Task<string> CreateGame(GameSettings gameSettings, IEnumerable<string> playerNames);
    Task<IReadOnlyCollection<Player>> GetPlayers(string gameId);
    Task<GameSettings> GetSettings(string gameId);
    Task<IReadOnlyDictionary<Coordinates, ushort>> GetMatterPerCoordinates(string gameId);
}
