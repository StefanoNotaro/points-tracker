using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

/// <summary>
/// Immutable record of an effective-role transition (or attempted transition,
/// in the case of <see cref="RoleAuditEventType.DriftDetected"/>). See
/// docs/ROLES_PERMISSIONS.md — Role-change audit trail.
/// </summary>
public class RoleAuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required GlobalRole? FromRole { get; init; }
    public required GlobalRole ToRole { get; init; }
    public required RoleAuditEventType Source { get; init; }
    public required string Actor { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    private RoleAuditLog() { }

    public static RoleAuditLog Record(
        Guid userId,
        GlobalRole? fromRole,
        GlobalRole toRole,
        RoleAuditEventType source,
        string actor,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required for audit entries.", nameof(actor));

        return new RoleAuditLog
        {
            UserId = userId,
            FromRole = fromRole,
            ToRole = toRole,
            Source = source,
            Actor = actor,
            Reason = reason
        };
    }
}
