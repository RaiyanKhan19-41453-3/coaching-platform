using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoachingPlatform.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            return await next();
        }
        finally
        {
            sw.Stop();
            logger.LogInformation("{Request} completed in {ElapsedMs} ms", name, sw.ElapsedMilliseconds);
        }
    }
}
