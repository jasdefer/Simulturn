using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace SimulturnApplication.Common.Behaviour;
public class LoggingBehavior<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Simulturn applicatoin request: {Name} {@Request}",
            requestName, request);
        return Task.CompletedTask;
    }
}