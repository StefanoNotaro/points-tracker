using System.Security.Claims;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        // The infrastructure layer stores the internal Guid ID in the "pts_id" claim after sync
        var value = principal?.FindFirstValue("pts_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the effective global role attached to the principal. The
    /// <c>pts_role</c> claim is a per-request snapshot of the DB-persisted
    /// effective role (see docs/ROLES_PERMISSIONS.md — Authority model).
    /// </summary>
    public static GlobalRole GetGlobalRole(this ClaimsPrincipal? principal)
    {
        var raw = principal?.FindFirstValue("pts_role");
        return GlobalRoleExtensions.TryParseGlobalRole(raw, out var role) ? role : GlobalRole.User;
    }

    public static bool HasRole(this ClaimsPrincipal? principal, GlobalRole required) =>
        principal.GetGlobalRole().IsAtLeast(required);
}
