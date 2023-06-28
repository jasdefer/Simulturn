using SimulturnApplication.Common.Interfaces;
using SimulturnInfrastructure.Persistance.InMemory;

namespace SimulturnInfrastructure.UnitTests.PersistenceTests.GameRepositoryTests;
public class InMemoryRepositoryTests : GameRepositoryFixture
{
    protected override IGameRepository GetGameRepository()
    {
        return new InMemoryGameRepository();
    }
}
