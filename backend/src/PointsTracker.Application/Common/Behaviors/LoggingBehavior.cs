using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PointsTracker.Application.Common.Behaviors;

/// <summary>
/// Logs the entry, success, and failure of every MediatR request along with
/// elapsed time. The request object itself is never logged — only its type
/// name — because commands and queries carry user IDs, share tokens, emails,
/// and other potentially sensitive fields.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Handling {Request}", name);
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {Request} in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (ValidationException vex)
        {
            sw.Stop();
            logger.LogWarning(
                vex,
                "Validation failed for {Request} after {Elapsed}ms",
                name,
                sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "Unhandled exception in {Request} after {Elapsed}ms",
                name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
