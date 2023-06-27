using Microsoft.Extensions.DependencyInjection;
using SimulturnApplication.Common.Interfaces;

namespace SimulturnInfrastructure;
public class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserService, InMemoryMasterDataRepository>();
        services.AddSingleton<IGameRepository, MasterDataLibrary>();
        services.AddSingleton<IPlayerRepository, SyncfusionStreamToDataConverter>();

        return services;
    }
}
