using SimulturnApplication.Common.Interfaces;

namespace SimulturnInfrastructure.Persistance.InMemory;
public class InMemoryCurrentUserService : ICurrentUserService
{
    public string UserId { get; set; } = "Player01";
}
