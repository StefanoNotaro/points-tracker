using FluentValidation;
using MediatR;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Admin.Commands;

/// <summary>
/// Manually overrides a user's effective global role. Used by super_admins to
/// promote/demote/revoke roles independently of the IdP. Always sets
/// <see cref="RoleSource.ManualOverride"/>, which pins the role against future
/// IdP claim drift until another admin clears the override.
/// See docs/ROLES_PERMISSIONS.md — Role Escalation Policy.
/// </summary>
public record ChangeUserRoleCommand(
    Guid TargetUserId,
    GlobalRole NewRole,
    Guid ActorUserId,
    string? Reason,
    bool Confirm
) : IRequest<ChangeUserRoleResult>;

public record ChangeUserRoleResult(
    Guid UserId,
    GlobalRole FromRole,
    GlobalRole ToRole,
    RoleSource Source,
    DateTime ChangedAt);

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(1024);
        RuleFor(x => x.Confirm)
            .Equal(true)
            .WithMessage("Role change must be explicitly confirmed (set 'confirm' to true).");
    }
}

public class ChangeUserRoleHandler(
    IUserRepository userRepo,
    IRoleAuditLogRepository auditRepo
) : IRequestHandler<ChangeUserRoleCommand, ChangeUserRoleResult>
{
    public async Task<ChangeUserRoleResult> Handle(ChangeUserRoleCommand cmd, CancellationToken ct)
    {
        var target = await userRepo.GetByIdAsync(cmd.TargetUserId, ct)
            ?? throw new NotFoundException(nameof(User), cmd.TargetUserId);

        var fromRole = target.Role;

        // Last-super_admin guard: a manual demotion of the only active
        // super_admin must be refused, even when the actor confirms.
        if (fromRole == GlobalRole.SuperAdmin && cmd.NewRole != GlobalRole.SuperAdmin)
        {
            var activeSuperAdmins = await userRepo.CountActiveSuperAdminsAsync(ct);
            if (activeSuperAdmins <= 1)
                throw new LastSuperAdminException();
        }

        var actor = $"admin:{cmd.ActorUserId}";
        var changed = target.SetRole(cmd.NewRole, RoleSource.ManualOverride, actor);

        if (changed)
        {
            await auditRepo.AddAsync(
                RoleAuditLog.Record(
                    target.Id,
                    fromRole: fromRole,
                    toRole: cmd.NewRole,
                    source: RoleAuditEventType.ManualOverride,
                    actor: actor,
                    reason: cmd.Reason),
                ct);
            await userRepo.SaveChangesAsync(ct);
        }

        return new ChangeUserRoleResult(
            target.Id,
            fromRole,
            target.Role,
            target.RoleSource,
            target.RoleUpdatedAt);
    }
}
