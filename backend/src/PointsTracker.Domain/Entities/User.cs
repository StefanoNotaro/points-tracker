using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string ExternalId { get; init; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public GlobalRole Role { get; private set; } = GlobalRole.User;
    public RoleSource RoleSource { get; private set; } = RoleSource.Default;
    public DateTime RoleUpdatedAt { get; private set; } = DateTime.UtcNow;
    public string RoleUpdatedBy { get; private set; } = "system";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    private User() { }

    public static User Create(string externalId, string email, string displayName) =>
        new() { ExternalId = externalId, Email = email, DisplayName = displayName };

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    /// <summary>
    /// Applies a role transition with its provenance. Idempotent when both role
    /// and source already match. Callers in the application layer are
    /// responsible for invariants that require I/O (e.g. last-super_admin
    /// guard) and for emitting the corresponding audit log entry.
    /// </summary>
    public bool SetRole(GlobalRole newRole, RoleSource source, string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required for role changes.", nameof(actor));

        if (Role == newRole && RoleSource == source)
            return false;

        Role = newRole;
        RoleSource = source;
        RoleUpdatedAt = DateTime.UtcNow;
        RoleUpdatedBy = actor;
        Touch();
        return true;
    }
}
