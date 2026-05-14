using MediatR;
using PointsTracker.Api.Extensions;
using PointsTracker.Application.Admin.Commands;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        group.MapPatch("/users/{id:guid}/role", ChangeUserRole).RequireRateLimiting("write");

        // Cleanup endpoints. See docs/ADMIN_CLEANUP.md.
        var cleanup = group.MapGroup("/cleanup");
        cleanup.MapGet("/preview", PreviewCleanup);
        cleanup.MapGet("/audit", GetCleanupAudit);
        cleanup.MapPost("/run-policy", RunCleanupPolicy).RequireRateLimiting("write");
        cleanup.MapPost("/soft-delete/counters", SoftDeleteCounters).RequireRateLimiting("write");
        cleanup.MapPost("/soft-delete/tournaments", SoftDeleteTournaments).RequireRateLimiting("write");
        cleanup.MapPost("/hard-purge/counters", HardPurgeCounters).RequireRateLimiting("write");
        cleanup.MapPost("/hard-purge/tournaments", HardPurgeTournaments).RequireRateLimiting("write");
        cleanup.MapPost("/share-tokens/purge-expired", PurgeExpiredShareTokens).RequireRateLimiting("write");
    }

    public record ChangeUserRoleRequest(string Role, string? Reason, bool Confirm);

    private static async Task<IResult> ChangeUserRole(
        Guid id,
        ChangeUserRoleRequest req,
        IMediator mediator,
        HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.SuperAdmin))
            return Results.Forbid();

        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();

        if (!GlobalRoleExtensions.TryParseGlobalRole(req.Role, out var newRole))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = [$"Unknown role '{req.Role}'. Allowed: user, admin, super_admin."]
            });

        var result = await mediator.Send(new ChangeUserRoleCommand(
            id, newRole, actorId.Value, req.Reason, req.Confirm));

        return Results.Ok(new
        {
            userId = result.UserId,
            fromRole = result.FromRole.ToWireString(),
            toRole = result.ToRole.ToWireString(),
            source = result.Source.ToWireString(),
            changedAt = result.ChangedAt
        });
    }

    // ---- Cleanup --------------------------------------------------------

    public record IdsRequest(IReadOnlyCollection<Guid> Ids, bool Confirm, string? Reason);
    public record ConfirmReasonRequest(bool Confirm, string? Reason);
    public record ConfirmRequest(bool Confirm);

    private static async Task<IResult> PreviewCleanup(IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var preview = await mediator.Send(new GetCleanupPreviewQuery());
        return Results.Ok(preview);
    }

    private static async Task<IResult> GetCleanupAudit(IMediator mediator, HttpContext ctx, int? take)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var rows = await mediator.Send(new GetCleanupAuditLogQuery(take ?? 100));
        return Results.Ok(rows.Select(r => new
        {
            id = r.Id,
            action = r.Action.ToWireString(),
            actor = r.Actor,
            targetCount = r.TargetCount,
            targetIdsJson = r.TargetIdsJson,
            reason = r.Reason,
            occurredAt = r.OccurredAt,
        }));
    }

    private static async Task<IResult> RunCleanupPolicy(
        ConfirmReasonRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        var result = await mediator.Send(new RunCleanupPolicyCommand(actorId.Value, req.Confirm, req.Reason));
        return Results.Ok(result);
    }

    private static async Task<IResult> SoftDeleteCounters(
        IdsRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        var affected = await mediator.Send(
            new SoftDeleteCountersCommand(req.Ids, actorId.Value, req.Confirm, req.Reason));
        return Results.Ok(new { affected });
    }

    private static async Task<IResult> SoftDeleteTournaments(
        IdsRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        var affected = await mediator.Send(
            new SoftDeleteTournamentsCommand(req.Ids, actorId.Value, req.Confirm, req.Reason));
        return Results.Ok(new { affected });
    }

    private static async Task<IResult> HardPurgeCounters(
        IdsRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.SuperAdmin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Reason))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Hard-purge requires a reason."]
            });
        var affected = await mediator.Send(
            new HardPurgeCountersCommand(req.Ids, actorId.Value, req.Confirm, req.Reason!));
        return Results.Ok(new { affected });
    }

    private static async Task<IResult> HardPurgeTournaments(
        IdsRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.SuperAdmin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Reason))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Hard-purge requires a reason."]
            });
        var affected = await mediator.Send(
            new HardPurgeTournamentsCommand(req.Ids, actorId.Value, req.Confirm, req.Reason!));
        return Results.Ok(new { affected });
    }

    private static async Task<IResult> PurgeExpiredShareTokens(
        ConfirmRequest req, IMediator mediator, HttpContext ctx)
    {
        if (!ctx.User.HasRole(GlobalRole.Admin)) return Results.Forbid();
        var actorId = ctx.User.GetUserId();
        if (actorId is null) return Results.Unauthorized();
        var affected = await mediator.Send(new PurgeExpiredShareTokensCommand(actorId.Value, req.Confirm));
        return Results.Ok(new { affected });
    }
}
