namespace PointsTracker.Domain.Enums;

/// <summary>
/// Kinds of cleanup operations recorded in <c>cleanup_audit_log</c>.
/// See docs/ADMIN_CLEANUP.md — Audit log.
/// </summary>
public enum CleanupAction
{
    SoftDeleteCounters,
    SoftDeleteTournaments,
    HardPurgeCounters,
    HardPurgeTournaments,
    PurgeExpiredShareTokens,
    RunPolicy
}

public static class CleanupActionExtensions
{
    public static string ToWireString(this CleanupAction a) => a switch
    {
        CleanupAction.SoftDeleteCounters => "soft_delete_counters",
        CleanupAction.SoftDeleteTournaments => "soft_delete_tournaments",
        CleanupAction.HardPurgeCounters => "hard_purge_counters",
        CleanupAction.HardPurgeTournaments => "hard_purge_tournaments",
        CleanupAction.PurgeExpiredShareTokens => "purge_expired_share_tokens",
        CleanupAction.RunPolicy => "run_policy",
        _ => throw new ArgumentOutOfRangeException(nameof(a), a, null)
    };

    public static CleanupAction ParseCleanupAction(string value) => value switch
    {
        "soft_delete_counters" => CleanupAction.SoftDeleteCounters,
        "soft_delete_tournaments" => CleanupAction.SoftDeleteTournaments,
        "hard_purge_counters" => CleanupAction.HardPurgeCounters,
        "hard_purge_tournaments" => CleanupAction.HardPurgeTournaments,
        "purge_expired_share_tokens" => CleanupAction.PurgeExpiredShareTokens,
        "run_policy" => CleanupAction.RunPolicy,
        _ => throw new ArgumentException($"Unknown cleanup action '{value}'", nameof(value))
    };
}
