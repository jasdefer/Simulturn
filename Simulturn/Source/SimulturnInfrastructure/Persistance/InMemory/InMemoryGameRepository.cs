using SimulturnApplication.Common.Interfaces;
using SimulturnDomain.Entities;

namespace SimulturnInfrastructure.Persistance.InMemory;
public class InMemoryGameRepository : IGameRepository
{
    public Task Add(Game game)
    {
        throw new NotImplementedException();
    }
}
