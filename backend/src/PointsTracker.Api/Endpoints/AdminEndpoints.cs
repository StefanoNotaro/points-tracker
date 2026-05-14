using MediatR;
using PointsTracker.Api.Extensions;
using PointsTracker.Application.Admin.Commands;
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
}
