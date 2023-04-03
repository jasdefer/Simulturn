using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SimulturnApplication.Common.Behaviour;
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private static readonly TimeSpan _longRequestDuration = TimeSpan.FromMilliseconds(500);

    public PerformanceBehavior(
        ILogger<TRequest> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        if (_timer.Elapsed > _longRequestDuration)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning("Simulturn long running request: {Name} ({ElapsedMilliseconds} duration) {@Request}",
                requestName, _longRequestDuration, request);
        }

        return response;
    }
}