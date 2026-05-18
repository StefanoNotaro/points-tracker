using MediatR;
using Microsoft.AspNetCore.SignalR;
using PointsTracker.Api.Extensions;
using PointsTracker.Application.Counters.Commands;
using PointsTracker.Application.Tournaments.Commands;
using PointsTracker.Application.Tournaments.Queries;
using PointsTracker.Infrastructure.Hubs;

namespace PointsTracker.Api.Endpoints;

public static class TournamentEndpoints
{
    public static void MapTournamentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tournaments").WithTags("Tournaments");

        group.MapPost("/",                CreateTournament).AllowAnonymous().RequireRateLimiting("write");
        group.MapGet("/mine",             ListMine).RequireAuthorization().RequireRateLimiting("read");
        group.MapPost("/mine-anonymous",  ListMineAnonymous).AllowAnonymous().RequireRateLimiting("read");
        group.MapGet("/{id:guid}",        GetTournament).AllowAnonymous().RequireRateLimiting("read");
        group.MapPatch("/{id:guid}/rules", UpdateRules).AllowAnonymous().RequireRateLimiting("write");
        group.MapDelete("/{id:guid}",     DeleteTournament).AllowAnonymous().RequireRateLimiting("write");

        group.MapPost("/{id:guid}/participants",                  AddParticipant).AllowAnonymous().RequireRateLimiting("write");
        group.MapDelete("/{id:guid}/participants/{participantId:guid}", RemoveParticipant).AllowAnonymous().RequireRateLimiting("write");

        group.MapPost("/{id:guid}/start",                                                Start).AllowAnonymous().RequireRateLimiting("write");
        group.MapPost("/{id:guid}/matches/{matchId:guid}/counter",                       OpenMatchCounter).AllowAnonymous().RequireRateLimiting("write");
        group.MapPost("/{id:guid}/matches/{matchId:guid}/result",                        RecordResult).AllowAnonymous().RequireRateLimiting("write");

        group.MapPost("/{id:guid}/matches/{matchId:guid}/scorer-links",             IssueMatchScorerLink).RequireAuthorization().RequireRateLimiting("scorer-link-issue");
        group.MapDelete("/{id:guid}/scorer-links/{linkId:guid}",                    RevokeMatchScorerLink).RequireAuthorization().RequireRateLimiting("write");
        group.MapGet("/{id:guid}/matches/{matchId:guid}/scorer-links",              ListMatchScorerLinks).RequireAuthorization().RequireRateLimiting("read");
    }

    private static async Task<IResult> CreateTournament(
        CreateTournamentRequest req,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var userId       = ctx.User.GetUserId();
        var sessionToken = ctx.Request.Headers["X-Session-Token"].FirstOrDefault();

        var custom = req.CustomRules is null ? null : new CustomRulesInput(
            req.CustomRules.PointsPerSet, req.CustomRules.LastSetPoints,
            req.CustomRules.SetsToWin,    req.CustomRules.TotalSets,
            req.CustomRules.WinByTwo);

        var result = await mediator.Send(new CreateTournamentCommand(
            req.Name, req.SportType, req.Format, userId, sessionToken,
            custom, req.IndoorSwitchEverySets,
            req.BeachAutoSwitchSides ?? true,
            req.CustomTimeoutsPerSet, req.CustomTimeoutDurationSeconds,
            req.GroupCount, req.AdvancePerGroup));

        return Results.Created($"/api/tournaments/{result.Tournament.Id}", result);
    }

    private static async Task<IResult> ListMine(IMediator mediator, HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        if (userId is null) return Results.Unauthorized();
        var list = await mediator.Send(new ListMyTournamentsQuery(userId.Value));
        return Results.Ok(list);
    }

    private static async Task<IResult> ListMineAnonymous(
        ListAnonymousTournamentsRequest req, IMediator mediator)
    {
        var list = await mediator.Send(
            new ListAnonymousTournamentsQuery(req.SessionTokens ?? []));
        return Results.Ok(list);
    }

    private static async Task<IResult> GetTournament(Guid id, IMediator mediator, HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var dto = await mediator.Send(new GetTournamentByIdQuery(id, userId, sessionToken));
        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateRules(
        Guid id,
        UpdateTournamentRulesRequest req,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var custom = req.CustomRules is null ? null : new CustomRulesInput(
            req.CustomRules.PointsPerSet, req.CustomRules.LastSetPoints,
            req.CustomRules.SetsToWin,    req.CustomRules.TotalSets,
            req.CustomRules.WinByTwo);
        var finalRules = req.FinalRules is null ? null : new CustomRulesInput(
            req.FinalRules.PointsPerSet, req.FinalRules.LastSetPoints,
            req.FinalRules.SetsToWin, req.FinalRules.TotalSets, req.FinalRules.WinByTwo);
        var semiRules = req.SemifinalRules is null ? null : new CustomRulesInput(
            req.SemifinalRules.PointsPerSet, req.SemifinalRules.LastSetPoints,
            req.SemifinalRules.SetsToWin, req.SemifinalRules.TotalSets, req.SemifinalRules.WinByTwo);

        var dto = await mediator.Send(new UpdateTournamentRulesCommand(
            id,
            req.Name,
            req.StartsAt, req.EndsAt,
            req.ClearStartsAt ?? false, req.ClearEndsAt ?? false,
            custom,
            req.ClearCustomRules ?? false,
            req.IndoorSwitchEverySets,
            req.BeachAutoSwitchSides ?? true,
            req.CustomTimeoutsPerSet, req.CustomTimeoutDurationSeconds,
            finalRules, req.FinalTimeoutsPerSet, req.FinalTimeoutDurationSeconds, req.ClearFinalRules ?? false,
            semiRules,  req.SemifinalTimeoutsPerSet, req.SemifinalTimeoutDurationSeconds, req.ClearSemifinalRules ?? false,
            userId, sessionToken));
        await hub.BroadcastTournamentUpdate(id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteTournament(Guid id, IMediator mediator, IHubContext<TournamentHub> hub, HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        await mediator.Send(new DeleteTournamentCommand(id, userId, sessionToken));
        await hub.BroadcastTournamentDeleted(id);
        return Results.NoContent();
    }

    private static async Task<IResult> AddParticipant(
        Guid id,
        AddParticipantRequest req,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var dto = await mediator.Send(new AddParticipantCommand(
            id, req.TeamName, req.Seed, req.UserId, userId, sessionToken));
        await hub.BroadcastTournamentUpdate(id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> RemoveParticipant(
        Guid id, Guid participantId,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var dto = await mediator.Send(new RemoveParticipantCommand(id, participantId, userId, sessionToken));
        await hub.BroadcastTournamentUpdate(id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> Start(
        Guid id,
        StartTournamentRequest? req,
        IMediator mediator, IHubContext<TournamentHub> hub, HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var dto = await mediator.Send(new StartTournamentCommand(
            id, userId, sessionToken,
            RandomizeUnseeded: req?.RandomizeUnseeded ?? false));
        await hub.BroadcastTournamentUpdate(id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> OpenMatchCounter(
        Guid id, Guid matchId,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var scorerToken = ctx.Request.Headers["X-Scorer-Token"].FirstOrDefault();
        var counterDto = await mediator.Send(new OpenMatchCounterCommand(id, matchId, userId, sessionToken, scorerToken));
        return Results.Ok(counterDto);
    }

    private static async Task<IResult> RecordResult(
        Guid id, Guid matchId,
        RecordMatchResultRequest req,
        IMediator mediator,
        IHubContext<TournamentHub> hub,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        var dto = await mediator.Send(new RecordMatchResultCommand(id, matchId, req.WinnerParticipantId, userId, sessionToken));
        await hub.BroadcastTournamentUpdate(id, dto);
        return Results.Ok(dto);
    }

    private static async Task<IResult> IssueMatchScorerLink(
        Guid id, Guid matchId,
        IssueMatchScorerLinkRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        if (userId is null) return Results.Unauthorized();
        var dto = await mediator.Send(new IssueMatchScorerLinkCommand(
            id, matchId, userId.Value, sessionToken, req.Label, req.GrantToUserId));
        return Results.Created($"/api/tournaments/{id}/matches/{matchId}/scorer-links/{dto.Id}", dto);
    }

    private static async Task<IResult> RevokeMatchScorerLink(
        Guid id, Guid linkId,
        IMediator mediator,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        if (userId is null) return Results.Unauthorized();
        await mediator.Send(new RevokeMatchScorerLinkCommand(id, linkId, userId.Value, sessionToken));
        return Results.NoContent();
    }

    private static async Task<IResult> ListMatchScorerLinks(
        Guid id, Guid matchId,
        IMediator mediator,
        HttpContext ctx)
    {
        var (userId, sessionToken) = GetContext(ctx);
        if (userId is null) return Results.Unauthorized();
        var list = await mediator.Send(new ListMatchScorerLinksQuery(id, matchId, userId.Value, sessionToken));
        return Results.Ok(list);
    }

    private static (Guid? userId, string? sessionToken) GetContext(HttpContext ctx) =>
        (ctx.User.GetUserId(), ctx.Request.Headers["X-Session-Token"].FirstOrDefault());
}

public record CreateTournamentRequest(
    string Name,
    string SportType,
    string Format,
    CustomRulesPayload? CustomRules,
    int? IndoorSwitchEverySets,
    bool? BeachAutoSwitchSides,
    int? CustomTimeoutsPerSet,
    int? CustomTimeoutDurationSeconds,
    int? GroupCount,
    int? AdvancePerGroup);

public record UpdateTournamentRulesRequest(
    string? Name,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool? ClearStartsAt,
    bool? ClearEndsAt,
    CustomRulesPayload? CustomRules,
    bool? ClearCustomRules,
    int? IndoorSwitchEverySets,
    bool? BeachAutoSwitchSides,
    int? CustomTimeoutsPerSet,
    int? CustomTimeoutDurationSeconds,
    CustomRulesPayload? FinalRules,
    int? FinalTimeoutsPerSet,
    int? FinalTimeoutDurationSeconds,
    bool? ClearFinalRules,
    CustomRulesPayload? SemifinalRules,
    int? SemifinalTimeoutsPerSet,
    int? SemifinalTimeoutDurationSeconds,
    bool? ClearSemifinalRules);

public record AddParticipantRequest(string TeamName, int? Seed, Guid? UserId);
public record RecordMatchResultRequest(Guid WinnerParticipantId);
public record ListAnonymousTournamentsRequest(List<string>? SessionTokens);
public record StartTournamentRequest(bool? RandomizeUnseeded);
public record IssueMatchScorerLinkRequest(string? Label, Guid? GrantToUserId);
