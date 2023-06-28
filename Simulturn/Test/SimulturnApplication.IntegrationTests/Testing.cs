using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimulturnApplication.Common.Interfaces;
using SimulturnInfrastructure;

namespace SimulturnApplication.IntegrationTests;
internal static class Testing
{
    internal static ISender GetSender(ICurrentUserService currentUserService)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((context, services) =>
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices();
            services.AddSingleton(currentUserService);
        });
        IHost app = builder.Build();
        ISender sender = app.Services.GetRequiredService<ISender>();
        return sender;
    }
}
