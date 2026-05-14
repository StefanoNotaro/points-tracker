namespace PointsTracker.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would leave zero active super_admin accounts.
/// See docs/ROLES_PERMISSIONS.md — "Role Escalation Policy".
/// </summary>
public class LastSuperAdminException()
    : DomainException("Operation would leave the platform without an active super_admin.");
