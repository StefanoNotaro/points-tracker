using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

/// <summary>
/// Append-only record of an admin- or system-triggered cleanup action.
/// See docs/ADMIN_CLEANUP.md — Audit log. Never store PII: ids and counts only.
/// </summary>
public class CleanupAuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required CleanupAction Action { get; init; }
    public required string Actor { get; init; }
    public required int TargetCount { get; init; }
    public string? TargetIdsJson { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    private CleanupAuditLog() { }

    public static CleanupAuditLog Record(
        CleanupAction action,
        string actor,
        int targetCount,
        IReadOnlyCollection<Guid>? targetIds = null,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required for audit entries.", nameof(actor));
        if (targetCount < 0)
            throw new ArgumentOutOfRangeException(nameof(targetCount));

        // Cap the id sample to keep the JSON column small — see ADMIN_CLEANUP.md.
        const int sampleCap = 50;
        var json = targetIds is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(
                targetIds.Count <= sampleCap ? targetIds : targetIds.Take(sampleCap).ToArray())
            : null;

        return new CleanupAuditLog
        {
            Action = action,
            Actor = actor,
            TargetCount = targetCount,
            TargetIdsJson = json,
            Reason = reason
        };
    }
}
