using SimulturnDomain.Entities;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Common.Interfaces;
public interface IGameRepository
{
    Task<bool> AllPlayersEndedTheirTurn(string gameId);
    Task<string> CreateGame(GameSettings gameSettings, IEnumerable<string> playerNames);
    Task<Game> GetGame(string gameId);
    Task SubtractMatter(IReadOnlyDictionary<Coordinates, ushort> matterPerCoordinates);
}
