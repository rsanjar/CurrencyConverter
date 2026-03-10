using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request execution details
/// including request name, execution time, and outcome.
/// Logs a warning when execution exceeds 500ms.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                logger.LogWarning("Long running request: {RequestName} ({ElapsedMs}ms)",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
