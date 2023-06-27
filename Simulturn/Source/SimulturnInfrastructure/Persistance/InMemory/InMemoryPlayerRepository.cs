using SimulturnApplication.Common.Interfaces;

namespace SimulturnInfrastructure.Persistance.InMemory;
internal class InMemoryPlayerRepository : IPlayerRepository
{
    public Task EndTurn(string gameId, string player)
    {
        throw new NotImplementedException();
    }
}
