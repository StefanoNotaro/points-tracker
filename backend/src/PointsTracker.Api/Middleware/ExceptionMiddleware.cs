using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Api.Middleware;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger,
    IProblemDetailsService problemDetailsService)
{
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

        if (ex is not (ValidationException or DbUpdateConcurrencyException or NotFoundException
                       or ForbiddenException or DomainException))
            logger.LogError(ex, "Unhandled exception");

        if (ex is ValidationException ve)
        {
            var errors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var validationProblem = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Validation failed",
                Type   = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            };

            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext    = ctx,
                ProblemDetails = validationProblem,
            });
            return;
        }

        var (statusCode, title) = ex switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "Resource was modified by another operation. Please refresh and retry."),
            NotFoundException   => (StatusCodes.Status404NotFound, ex.Message),
            ForbiddenException  => (StatusCodes.Status403Forbidden, ex.Message),
            DomainException     => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            _                   => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        ctx.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext    = ctx,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title },
        });
    }
}
