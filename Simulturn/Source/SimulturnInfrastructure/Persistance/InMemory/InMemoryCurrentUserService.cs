using SimulturnApplication.Common.Interfaces;

namespace SimulturnInfrastructure.Persistance.InMemory;
internal class InMemoryCurrentUserService : ICurrentUserService
{
    public string UserId { get; }
}
