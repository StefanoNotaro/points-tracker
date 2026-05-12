using System.Security.Claims;

namespace PointsTracker.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        // The infrastructure layer stores the internal Guid ID in the "pts_id" claim after sync
        var value = principal?.FindFirstValue("pts_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static bool HasRole(this ClaimsPrincipal? principal, string role)
    {
        var hierarchy = new[] { "user", "admin", "super_admin" };
        var userRole = principal?.FindFirstValue("pts_role") ?? "user";
        return Array.IndexOf(hierarchy, userRole) >= Array.IndexOf(hierarchy, role);
    }
}
