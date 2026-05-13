using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            await HandleAsync(ctx, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, errors) = ex switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "Validation failed",
                ve.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message, (Dictionary<string, string[]>?)null),
            ForbiddenException => (HttpStatusCode.Forbidden, ex.Message, null),
            DomainException => (HttpStatusCode.UnprocessableEntity, ex.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (ex is DbUpdateConcurrencyException dbEx)
        {
            foreach (var entry in dbEx.Entries)
            {
                var pk = entry.Metadata.FindPrimaryKey();
                var pkValue = pk is null ? "?" : string.Join(",", pk.Properties
                    .Select(p => entry.OriginalValues[p]?.ToString() ?? "null"));
                logger.LogError("Concurrency conflict on {Entity} pk={PrimaryKey} state={State}",
                    entry.Entity.GetType().Name, pkValue, entry.State);
            }
        }

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(ex, "Unhandled exception");

        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{(int)status}",
            ["title"] = title,
            ["status"] = (int)status,
        };

        if (errors is not null) problem["errors"] = errors;

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
