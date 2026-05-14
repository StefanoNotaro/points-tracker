using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Services;

/// <summary>
/// Concrete implementation of <see cref="ICleanupService"/>. All writes use
/// EF Core's set-based <c>ExecuteUpdateAsync</c> / <c>ExecuteDeleteAsync</c>
/// to avoid pulling whole entities into memory.
/// </summary>
public class CleanupService(
    AppDbContext db,
    IOptionsMonitor<CounterCleanupOptions> options) : ICleanupService
{
    private const int SampleSize = 50;

    public async Task<CleanupPreview> PreviewAsync(CancellationToken ct = default)
    {
        var opts = options.CurrentValue;
        var now = DateTime.UtcNow;
        var softCutoff = now - TimeSpan.FromDays(opts.AnonymousInactiveDays);
        var hardCutoff = now - TimeSpan.FromDays(opts.HardDeleteGraceDays);
        var completedCutoff = now - TimeSpan.FromDays(opts.TournamentCompletedRetentionDays);

        var staleCountersQuery = db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null
                        && c.OwnerUserId == null
                        && c.UpdatedAt < softCutoff);

        var staleTournamentsQuery = db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt == null
                        && t.OwnerUserId == null
                        && (t.UpdatedAt < softCutoff
                            || ((t.Status == TournamentStatus.Completed
                                 || t.Status == TournamentStatus.Abandoned)
                                && t.UpdatedAt < completedCutoff)));

        var staleCounters = await staleCountersQuery.CountAsync(ct);
        var staleTournaments = await staleTournamentsQuery.CountAsync(ct);

        var expiredTokens = await db.ShareTokens
            .Where(t => t.RevokedAt != null || t.ExpiresAt < now)
            .CountAsync(ct);

        var countersPastGrace = await db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && c.DeletedAt < hardCutoff)
            .CountAsync(ct);

        var tournamentsPastGrace = await db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt != null && t.DeletedAt < hardCutoff)
            .CountAsync(ct);

        var sampleCounters = await staleCountersQuery
            .OrderBy(c => c.UpdatedAt)
            .Select(c => c.Id)
            .Take(SampleSize)
            .ToListAsync(ct);

        var sampleTournaments = await staleTournamentsQuery
            .OrderBy(t => t.UpdatedAt)
            .Select(t => t.Id)
            .Take(SampleSize)
            .ToListAsync(ct);

        return new CleanupPreview(
            staleCounters, staleTournaments,
            expiredTokens, countersPastGrace, tournamentsPastGrace,
            sampleCounters, sampleTournaments);
    }

    public async Task<CleanupRunResult> RunPolicyAsync(CancellationToken ct = default)
    {
        var opts = options.CurrentValue;
        var now = DateTime.UtcNow;
        var softCutoff = now - TimeSpan.FromDays(opts.AnonymousInactiveDays);
        var hardCutoff = now - TimeSpan.FromDays(opts.HardDeleteGraceDays);
        var completedCutoff = now - TimeSpan.FromDays(opts.TournamentCompletedRetentionDays);

        // Phase 1 — soft-delete stale anonymous tournaments first so phase 2
        // can pick up their orphaned counters in the same sweep.
        var tournamentsSoft = await db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt == null
                        && t.OwnerUserId == null
                        && (t.UpdatedAt < softCutoff
                            || ((t.Status == TournamentStatus.Completed
                                 || t.Status == TournamentStatus.Abandoned)
                                && t.UpdatedAt < completedCutoff)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.DeletedAt, _ => now)
                .SetProperty(t => t.UpdatedAt, _ => now), ct);

        var countersSoft = await db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null
                        && ((c.OwnerUserId == null && c.UpdatedAt < softCutoff)
                            || (c.LinkedTournamentId != null
                                && db.Tournaments.IgnoreQueryFilters()
                                    .Any(t => t.Id == c.LinkedTournamentId && t.DeletedAt != null))))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeletedAt, _ => now)
                .SetProperty(c => c.UpdatedAt, _ => now), ct);

        // Phase 2 — hard-purge soft-deleted rows past the grace window. EF
        // cascades drop their children.
        var tournamentsHard = await db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt != null && t.DeletedAt < hardCutoff)
            .ExecuteDeleteAsync(ct);

        var countersHard = await db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && c.DeletedAt < hardCutoff)
            .ExecuteDeleteAsync(ct);

        // Phase 3 — expired/revoked share tokens. Independent of counter state
        // (a revoked token on a live counter should also disappear).
        var tokensPurged = await db.ShareTokens
            .Where(t => t.RevokedAt != null || t.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);

        return new CleanupRunResult(
            countersSoft, tournamentsSoft, countersHard, tournamentsHard, tokensPurged);
    }

    public async Task<int> SoftDeleteCountersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        var now = DateTime.UtcNow;
        return await db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null && ids.Contains(c.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeletedAt, _ => now)
                .SetProperty(c => c.UpdatedAt, _ => now), ct);
    }

    public async Task<int> SoftDeleteTournamentsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        var now = DateTime.UtcNow;
        return await db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt == null && ids.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.DeletedAt, _ => now)
                .SetProperty(t => t.UpdatedAt, _ => now), ct);
    }

    public async Task<int> HardPurgeCountersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        // Refuse to hard-purge a live row — soft-delete is the required first step.
        return await db.Counters.IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && ids.Contains(c.Id))
            .ExecuteDeleteAsync(ct);
    }

    public async Task<int> HardPurgeTournamentsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        return await db.Tournaments.IgnoreQueryFilters()
            .Where(t => t.DeletedAt != null && ids.Contains(t.Id))
            .ExecuteDeleteAsync(ct);
    }

    public async Task<int> PurgeExpiredShareTokensAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.ShareTokens
            .Where(t => t.RevokedAt != null || t.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);
    }
}
