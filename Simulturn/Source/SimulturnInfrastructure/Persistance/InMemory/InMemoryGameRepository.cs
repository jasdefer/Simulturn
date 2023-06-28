using SimulturnApplication.Common.Interfaces;
using SimulturnDomain.Entities;

namespace SimulturnInfrastructure.Persistance.InMemory;
public class InMemoryGameRepository : IGameRepository
{
    private readonly Dictionary<string, Game> _dict = new Dictionary<string, Game>();
    public Task Add(Game game)
    {
        _dict.Add(game.Id, game);
        return Task.CompletedTask;
    }

    public Task EndTurn(string gameId, string player)
    {
        _dict[gameId].Players.Single(x => x.Name.Equals(player)).HasEndedTurn = true;
        return Task.CompletedTask;
    }

    public Task<ushort> GetTurn(string gameId)
    {
        var result = _dict[gameId].Turn;
        return Task.FromResult(result);
    }

    public Task<bool> HasPlayerEndedHisTurn(string gameId, string playerId)
    {
        var result = _dict[gameId].Players.Single(x => x.Name.Equals(playerId)).HasEndedTurn;
        return Task.FromResult(result);
    }

    public Task<bool> HaveAllPlayersEndedTheirTurn(string gameId)
    {
        var allPlayersEndedTheirTurn = !_dict[gameId].Players.Any(x => x.HasEndedTurn == false);
        return Task.FromResult(allPlayersEndedTheirTurn);
    }

    public Task NewTurn(string gameId)
    {
        foreach (var player in _dict[gameId].Players)
        {
            player.HasEndedTurn = false;
        }
        _dict[gameId].Turn++;
        return Task.CompletedTask;
    }
}
