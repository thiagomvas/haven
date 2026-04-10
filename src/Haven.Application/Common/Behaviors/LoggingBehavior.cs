using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Common.Behaviors;
public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TMessage).Name;

        logger.LogDebug("Handling {RequestName} {@Request}", name, message);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(message, ct);
            stopwatch.Stop();

            if (response is Result { IsFailure: true } result)
            {
                logger.LogWarning(
                    "Request {RequestName} returned failure [{ErrorCode}]: {ErrorMessage} in {ElapsedMs}ms",
                    name, result.Error.Code, result.Error.Message, stopwatch.ElapsedMilliseconds);

                return response;
            }

            logger.LogDebug(
                "Handled {RequestName} successfully in {ElapsedMs}ms",
                name, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Request {RequestName} threw an unhandled exception after {ElapsedMs}ms",
                name, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}