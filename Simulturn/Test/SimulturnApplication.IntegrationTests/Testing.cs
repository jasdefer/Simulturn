using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimulturnInfrastructure;

namespace SimulturnApplication.IntegrationTests;
internal static class Testing
{
    internal static ISender GetSender()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((context, services) =>
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices();
        });
        IHost app = builder.Build();
        ISender sender = app.Services.GetRequiredService<ISender>();
        return sender;
    }
}
