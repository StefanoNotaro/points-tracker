namespace PointsTracker.Domain.Enums;

/// <summary>
/// Why a row was written to the role audit log. Broader than
/// <see cref="RoleSource"/> because it also covers the no-op "claim disagrees
/// with manual override" case (`DriftDetected`), where the persisted role does
/// not change but the disagreement is recorded for visibility.
/// </summary>
public enum RoleAuditEventType
{
    Default,
    IdpClaim,
    ManualOverride,
    DriftDetected
}

public static class RoleAuditEventTypeExtensions
{
    public static string ToWireString(this RoleAuditEventType type) => type switch
    {
        RoleAuditEventType.Default => "default",
        RoleAuditEventType.IdpClaim => "idp_claim",
        RoleAuditEventType.ManualOverride => "manual_override",
        RoleAuditEventType.DriftDetected => "drift_detected",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static RoleAuditEventType ParseRoleAuditEventType(string value) => value switch
    {
        "default" => RoleAuditEventType.Default,
        "idp_claim" => RoleAuditEventType.IdpClaim,
        "manual_override" => RoleAuditEventType.ManualOverride,
        "drift_detected" => RoleAuditEventType.DriftDetected,
        _ => throw new ArgumentException($"Unknown role audit event type '{value}'", nameof(value))
    };
}
