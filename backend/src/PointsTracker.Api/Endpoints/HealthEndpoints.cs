using Microsoft.EntityFrameworkCore;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

        app.MapGet("/health/ready", async (AppDbContext db) =>
        {
            var canConnect = await db.Database.CanConnectAsync();
            return canConnect
                ? Results.Ok(new { status = "Ready" })
                : Results.StatusCode(503);
        }).AllowAnonymous();
    }
}
