using Microsoft.Extensions.DependencyInjection;
using SimulturnApplication.Common.Interfaces;
using SimulturnInfrastructure.Persistance.InMemory;

namespace SimulturnInfrastructure;
public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserService, InMemoryCurrentUserService>();
        services.AddSingleton<IGameRepository, InMemoryGameRepository>();

        return services;
    }
}
