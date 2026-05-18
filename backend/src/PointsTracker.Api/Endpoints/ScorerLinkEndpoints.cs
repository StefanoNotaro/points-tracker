using MediatR;
using PointsTracker.Application.Tournaments.Queries;

namespace PointsTracker.Api.Endpoints;

public static class ScorerLinkEndpoints
{
    public static void MapScorerLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scorer-links").WithTags("ScorerLinks");

        // Resolves a raw scorer token to its associated tournament/match/counter IDs.
        // Anonymous — scorers are not required to be authenticated users.
        group.MapGet("/resolve/{token}", Resolve)
             .AllowAnonymous()
             .RequireRateLimiting("counter-join");
    }

    private static async Task<IResult> Resolve(string token, IMediator mediator)
    {
        var dto = await mediator.Send(new ResolveMatchScorerLinkQuery(token));
        return Results.Ok(dto);
    }
}
