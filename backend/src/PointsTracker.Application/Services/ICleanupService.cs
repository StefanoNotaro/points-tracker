namespace PointsTracker.Application.Services;

/// <summary>
/// Cleanup operations shared by the background worker and the admin
/// dashboard. Returns row counts; never throws on "nothing to do".
/// See docs/ADMIN_CLEANUP.md.
/// </summary>
public interface ICleanupService
{
    /// <summary>
    /// Compute candidate counts and id samples without writing anything.
    /// </summary>
    Task<CleanupPreview> PreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Soft-delete stale anonymous counters/tournaments, hard-purge soft-deleted
    /// rows past the grace window, and purge expired share tokens. Same code
    /// path the background worker calls. Idempotent.
    /// </summary>
    Task<CleanupRunResult> RunPolicyAsync(CancellationToken ct = default);

    /// <summary>
    /// Soft-delete the specified counters by id. Skips ids that are already
    /// soft-deleted or do not exist. Returns the count actually touched.
    /// </summary>
    Task<int> SoftDeleteCountersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<int> SoftDeleteTournamentsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Hard-purge the specified soft-deleted rows. Refuses ids whose
    /// <c>DeletedAt</c> is null (must soft-delete first).
    /// </summary>
    Task<int> HardPurgeCountersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<int> HardPurgeTournamentsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Delete all share tokens whose <c>ExpiresAt</c> is in the past or whose
    /// <c>RevokedAt</c> is set. Hard delete — no recovery.
    /// </summary>
    Task<int> PurgeExpiredShareTokensAsync(CancellationToken ct = default);
}

public sealed record CleanupPreview(
    int StaleAnonymousCounters,
    int StaleAnonymousTournaments,
    int ExpiredShareTokens,
    int CountersPastGrace,
    int TournamentsPastGrace,
    IReadOnlyList<Guid> SampleCounterIds,
    IReadOnlyList<Guid> SampleTournamentIds);

public sealed record CleanupRunResult(
    int CountersSoftDeleted,
    int TournamentsSoftDeleted,
    int CountersHardPurged,
    int TournamentsHardPurged,
    int ShareTokensPurged);
