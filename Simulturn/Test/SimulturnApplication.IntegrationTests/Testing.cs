using MediatR;
using Microsoft.Extensions.Hosting;
using SimulturnApplication;
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
            services.AddInfrastructureServices(context.Configuration);
        });
        IHost app = builder.Build();
        ISender sender = app.Services.GetRequiredService<ISender>();
        return sender;
    }
}
