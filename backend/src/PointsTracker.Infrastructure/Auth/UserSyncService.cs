using System.Security.Claims;
using Microsoft.Extensions.Logging;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Infrastructure.Auth;

/// <summary>
/// Persists OIDC identity into the local <c>users</c> table on every token
/// validation and reconciles the effective global role against the
/// <c>pts_roles</c> claim per the Option B authority model documented in
/// docs/ROLES_PERMISSIONS.md.
/// </summary>
public class UserSyncService(
    IUserRepository userRepo,
    IRoleAuditLogRepository roleAuditRepo,
    ILogger<UserSyncService> logger)
{
    public async Task<User> SyncAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No subject claim in token.");

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new InvalidOperationException("No email claim in token.");

        var displayName = principal.FindFirstValue("name") ?? email;
        var claimRole = ExtractRoleFromClaims(principal);
        var actor = $"idp:{externalId}";

        var user = await userRepo.GetByExternalIdAsync(externalId, ct);
        if (user is null)
        {
            user = User.Create(externalId, email, displayName);

            // First-login role: trust the IdP claim if present, else default.
            // The DB persisted role becomes authoritative from this point.
            if (claimRole is { } seededRole && seededRole != GlobalRole.User)
            {
                user.SetRole(seededRole, RoleSource.IdpClaim, actor);
            }

            await userRepo.AddAsync(user, ct);
            await roleAuditRepo.AddAsync(
                RoleAuditLog.Record(
                    user.Id,
                    fromRole: null,
                    toRole: user.Role,
                    source: claimRole is null
                        ? RoleAuditEventType.Default
                        : RoleAuditEventType.IdpClaim,
                    actor: actor),
                ct);
            await userRepo.SaveChangesAsync(ct);
            logger.LogInformation("Created new user {ExternalId} with role {Role}", externalId, user.Role);
            return user;
        }

        user.Email = email;
        user.DisplayName = displayName;
        user.Touch();

        await ReconcileRoleAsync(user, claimRole, actor, ct);
        await userRepo.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>
    /// Applies the Option B precedence rules:
    /// 1. manual_override wins — claim is ignored, mismatch logged as drift.
    /// 2. otherwise, the IdP claim is authoritative.
    /// 3. missing claim → keep persisted role (never silent demotion).
    /// 4. last-super_admin demotion is refused and recorded as drift.
    /// </summary>
    private async Task ReconcileRoleAsync(
        User user,
        GlobalRole? claimRole,
        string actor,
        CancellationToken ct)
    {
        if (user.RoleSource == RoleSource.ManualOverride)
        {
            if (claimRole is { } cr && cr != user.Role)
            {
                logger.LogInformation(
                    "Claim drift on manual_override for user {UserId}: persisted={Persisted} claim={Claim}",
                    user.Id, user.Role, cr);
                await roleAuditRepo.AddAsync(
                    RoleAuditLog.Record(
                        user.Id,
                        fromRole: user.Role,
                        toRole: cr,
                        source: RoleAuditEventType.DriftDetected,
                        actor: actor,
                        reason: "IdP claim disagrees with manual override; persisted role kept."),
                    ct);
            }
            return;
        }

        if (claimRole is null)
        {
            // Provider outage or claim misconfig — never auto-demote.
            return;
        }

        if (claimRole == user.Role && user.RoleSource == RoleSource.IdpClaim)
            return;

        var fromRole = user.Role;

        // Last-super_admin guard: refuse to demote the only super_admin via IdP,
        // but record the disagreement.
        if (fromRole == GlobalRole.SuperAdmin && claimRole != GlobalRole.SuperAdmin)
        {
            var activeSuperAdmins = await userRepo.CountActiveSuperAdminsAsync(ct);
            if (activeSuperAdmins <= 1)
            {
                logger.LogWarning(
                    "Refused IdP-driven demotion of last super_admin {UserId} (claim={Claim})",
                    user.Id, claimRole);
                await roleAuditRepo.AddAsync(
                    RoleAuditLog.Record(
                        user.Id,
                        fromRole: fromRole,
                        toRole: claimRole.Value,
                        source: RoleAuditEventType.DriftDetected,
                        actor: actor,
                        reason: "Refused: would leave platform without an active super_admin."),
                    ct);
                return;
            }
        }

        if (user.SetRole(claimRole.Value, RoleSource.IdpClaim, actor))
        {
            await roleAuditRepo.AddAsync(
                RoleAuditLog.Record(
                    user.Id,
                    fromRole: fromRole,
                    toRole: claimRole.Value,
                    source: RoleAuditEventType.IdpClaim,
                    actor: actor),
                ct);
            logger.LogInformation(
                "Synced role for user {UserId}: {From} -> {To} (idp_claim)",
                user.Id, fromRole, claimRole);
        }
    }

    /// <summary>
    /// Reads <c>pts_roles</c> (multi) and <c>pts_role</c> (single) claims and
    /// returns the highest matching role. Returns <c>null</c> when no claim is
    /// present — the caller treats this as "keep persisted role".
    /// </summary>
    private static GlobalRole? ExtractRoleFromClaims(ClaimsPrincipal principal)
    {
        var values = principal
            .FindAll("pts_roles")
            .Select(c => c.Value)
            .Concat(principal.FindAll("pts_role").Select(c => c.Value))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (values.Count == 0)
            return null;

        GlobalRole? best = null;
        foreach (var raw in values)
        {
            if (!GlobalRoleExtensions.TryParseGlobalRole(raw, out var parsed))
                continue;
            if (best is null || parsed.IsAtLeast(best.Value))
                best = parsed;
        }

        return best;
    }
}
