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
        group.MapGet("/mine", ListMine).RequireAuthorization();
        group.MapGet("/{id:guid}", GetCounter).AllowAnonymous();
        group.MapGet("/join/{token}", JoinByToken).AllowAnonymous();
        group.MapPost("/{id:guid}/score/increment", IncrementScore).AllowAnonymous();
        group.MapPost("/{id:guid}/score/decrement", DecrementScore).AllowAnonymous();
        group.MapPost("/{id:guid}/undo", Undo).AllowAnonymous();
        group.MapPost("/{id:guid}/redo", Redo).AllowAnonymous();
        group.MapPatch("/{id:guid}/teams", UpdateTeamName).AllowAnonymous();
        group.MapPost("/{id:guid}/side-switch", ResolveSideSwitch).AllowAnonymous();
        group.MapPost("/{id:guid}/switch-sides", SwitchSides).AllowAnonymous();
        group.MapPost("/{id:guid}/share", CreateShareToken).AllowAnonymous();
        group.MapDelete("/{id:guid}", DeleteCounter).AllowAnonymous();
    }

    private static async Task<IResult> ListMine(IMediator mediator, HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        if (userId is null) return Results.Unauthorized();
        var result = await mediator.Send(new ListMyCountersQuery(userId.Value));
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteCounter(
        Guid id,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();
        await mediator.Send(new DeleteCounterCommand(id, userId, sessionToken));
        return Results.NoContent();
    }

    private static async Task<IResult> CreateCounter(
        CreateCounterRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var customRules = req.CustomRules is null
            ? null
            : new CustomRulesInput(
                req.CustomRules.PointsPerSet,
                req.CustomRules.LastSetPoints,
                req.CustomRules.SetsToWin,
                req.CustomRules.TotalSets,
                req.CustomRules.WinByTwo);
        var result = await mediator.Send(new CreateCounterCommand(req.SportType, req.TeamAName, req.TeamBName, userId, customRules, req.IndoorSwitchEverySets, req.BeachAutoSwitchSides ?? true));
        return Results.Created($"/api/counters/{result.Counter.Id}", result);
    }

    private static async Task<IResult> GetCounter(
        Guid id,
        IMediator mediator,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new GetCounterByIdQuery(id, userId, sessionToken, shareToken));
        return Results.Ok(result);
    }

    private static async Task<IResult> JoinByToken(
        string token,
        IMediator mediator,
        HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var result = await mediator.Send(new JoinByShareTokenQuery(token, userId));
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
        UndoRequest? req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var count = req?.Count ?? 1;
        var result = await mediator.Send(new UndoCommand(id, userId, sessionToken, shareToken, count));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> Redo(
        Guid id,
        UndoRequest? req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var count = req?.Count ?? 1;
        var result = await mediator.Send(new RedoCommand(id, userId, sessionToken, shareToken, count));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateTeamName(
        Guid id,
        UpdateTeamNameRequest req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new UpdateTeamNameCommand(id, req.Team, req.Name, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> ResolveSideSwitch(
        Guid id,
        ResolveSideSwitchRequest req,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new ResolveSideSwitchCommand(id, req.Confirm, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
        return Results.Ok(result);
    }

    private static async Task<IResult> SwitchSides(
        Guid id,
        IMediator mediator,
        IHubContext<CounterHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken, shareToken) = GetContext(ctx);
        var result = await mediator.Send(new SwitchSidesCommand(id, userId, sessionToken, shareToken));
        await hub.BroadcastScoreUpdate(id, result);
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
        var baseUrl = ResolveFrontendBaseUrl(ctx);
        var result = await mediator.Send(new CreateShareTokenCommand(id, req.Scope, userId, sessionToken, baseUrl));
        return Results.Ok(result);
    }

    private static string ResolveFrontendBaseUrl(HttpContext ctx)
    {
        // Prefer Frontend:BaseUrl from configuration (production), then the Origin / Referer header
        // sent by the browser, and finally fall back to the request's own host (rarely correct in dev,
        // but a safe last resort).
        var configured = ctx.RequestServices.GetService<IConfiguration>()?["Frontend:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');

        var origin = ctx.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin)) return origin.TrimEnd('/');

        var referer = ctx.Request.Headers.Referer.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
            return $"{refUri.Scheme}://{refUri.Authority}";

        return $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    }

    private static (Guid? userId, string? sessionToken, string? shareToken) GetContext(HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();
        // Header takes precedence; the legacy ?share= query param remains for direct deep-links.
        var shareToken = ctx.Request.Headers["X-Share-Token"].FirstOrDefault()
                         ?? ctx.Request.Query["share"].FirstOrDefault();
        return (userId, sessionToken, shareToken);
    }
}

public record CreateCounterRequest(
    string SportType,
    string TeamAName,
    string TeamBName,
    CustomRulesPayload? CustomRules,
    int? IndoorSwitchEverySets,
    bool? BeachAutoSwitchSides
);
public record CustomRulesPayload(int PointsPerSet, int LastSetPoints, int SetsToWin, int TotalSets, bool WinByTwo);
public record ScoreRequest(string Team);
public record UpdateTeamNameRequest(string Team, string Name);
public record CreateShareTokenRequest(string Scope);
public record ResolveSideSwitchRequest(bool Confirm);
public record UndoRequest(int? Count);
