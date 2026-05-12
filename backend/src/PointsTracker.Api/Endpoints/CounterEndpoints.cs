using MediatR;
using Microsoft.AspNetCore.SignalR;
using PointsTracker.Api.Extensions;
using PointsTracker.Application.Counters.Commands;
using PointsTracker.Application.Counters.Queries;
using PointsTracker.Infrastructure.Hubs;

namespace PointsTracker.Api.Endpoints;

public static class CounterEndpoints
{
    public static void MapCounterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/counters").WithTags("Counters");

        group.MapPost("/", CreateCounter).AllowAnonymous();
        group.MapGet("/{id:guid}", GetCounter).AllowAnonymous();
        group.MapGet("/join/{token}", JoinByToken).AllowAnonymous();
        group.MapPost("/{id:guid}/score/increment", IncrementScore).AllowAnonymous();
        group.MapPost("/{id:guid}/score/decrement", DecrementScore).AllowAnonymous();
        group.MapPost("/{id:guid}/undo", Undo).AllowAnonymous();
        group.MapPatch("/{id:guid}/teams", UpdateTeamName).AllowAnonymous();
        group.MapPost("/{id:guid}/share", CreateShareToken).AllowAnonymous();
    }

    private static async Task<IResult> CreateCounter(
        CreateCounterRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var result = await mediator.Send(new CreateCounterCommand(req.SportType, req.TeamAName, req.TeamBName, userId));
        return Results.Created($"/api/counters/{result.Counter.Id}", result);
    }

    private static async Task<IResult> GetCounter(
        Guid id,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();
        var shareToken = ctx.Request.Query["share"].FirstOrDefault();
        var result = await mediator.Send(new GetCounterByIdQuery(id, userId, sessionToken, shareToken));
        return Results.Ok(result);
    }

    private static async Task<IResult> JoinByToken(
        string token,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var result = await mediator.Send(new GetCounterByIdQuery(Guid.Empty, userId, null, token));
        return Results.Ok(result);
    }

    private static async Task<IResult> IncrementScore(
        Guid id,
        ScoreRequest req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new UpdateScoreCommand(id, req.Team, true, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> DecrementScore(
        Guid id,
        ScoreRequest req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new UpdateScoreCommand(id, req.Team, false, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> Undo(
        Guid id,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new UndoCommand(id, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateTeamName(
        Guid id,
        UpdateTeamNameRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        // Reuse UpdateScore mechanism — delegate to domain directly via command
        // For now returns current counter (future: dedicated command)
        var result = await mediator.Send(new GetCounterByIdQuery(id, userId, sessionToken, shareToken));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateShareToken(
        Guid id,
        CreateShareTokenRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();
        var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var result = await mediator.Send(new CreateShareTokenCommand(id, req.Scope, userId, sessionToken, baseUrl));
        return Results.Ok(result);
    }

    private static (Guid? userId, string? sessionToken, string? shareToken) GetContext(HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();
        var shareToken = ctx.Request.Query["share"].FirstOrDefault();
        return (userId, sessionToken, shareToken);
    }
}

public record CreateCounterRequest(string SportType, string TeamAName, string TeamBName);
public record ScoreRequest(string Team);
public record UpdateTeamNameRequest(string Team, string Name);
public record CreateShareTokenRequest(string Scope);
