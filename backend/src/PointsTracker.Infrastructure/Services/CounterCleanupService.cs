using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Services;

/// <summary>
/// Periodically removes orphaned anonymous counters and tournaments.
/// Authenticated users' data is out of scope — they manage it through the UI.
///
/// The cleanup runs in two phases for each entity type:
///   1. Soft-delete anonymous rows whose UpdatedAt is older than
///      <see cref="CounterCleanupOptions.AnonymousInactiveDays"/>. Soft-deleted
///      rows disappear from the API thanks to the global query filter.
///   2. Hard-delete any soft-deleted row whose DeletedAt is older than
///      <see cref="CounterCleanupOptions.HardDeleteGraceDays"/>. EF cascades
///      drop child rows (counter sets/events/share tokens, tournament
///      participants/matches) via the relational delete behavior.
/// </summary>
public class CounterCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<CounterCleanupOptions> options,
    ILogger<CounterCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.CurrentValue;
        if (!opts.Enabled)
        {
            logger.LogInformation("Counter cleanup service is disabled via configuration.");
            return;
        }

        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Counter cleanup pass failed.");
            }

            try { await Task.Delay(options.CurrentValue.Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var opts = options.CurrentValue;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var softDeleteCutoff = now - TimeSpan.FromDays(opts.AnonymousInactiveDays);
        var hardDeleteCutoff = now - TimeSpan.FromDays(opts.HardDeleteGraceDays);

        // Phase 1 — soft-delete stale anonymous tournaments first so that
        // their linked counters are caught by the counter sweep below.
        var tournamentsSoftDeleted = await db.Tournaments
            .IgnoreQueryFilters()
            .Where(t => t.DeletedAt == null
                        && t.OwnerUserId == null
                        && t.UpdatedAt < softDeleteCutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.DeletedAt, _ => now)
                .SetProperty(t => t.UpdatedAt, _ => now), ct);

        // Phase 2 — soft-delete counters that are either stale-and-anonymous
        // OR linked to an already-soft-deleted tournament. The second clause
        // catches counters spawned by tournament matches (which carry the
        // tournament's ownership, not their own session token) and ensures
        // they don't outlive their parent tournament.
        var countersSoftDeleted = await db.Counters
            .IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null
                        && ((c.OwnerUserId == null && c.UpdatedAt < softDeleteCutoff)
                            || (c.LinkedTournamentId != null
                                && db.Tournaments
                                    .IgnoreQueryFilters()
                                    .Any(t => t.Id == c.LinkedTournamentId && t.DeletedAt != null))))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeletedAt, _ => now)
                .SetProperty(c => c.UpdatedAt, _ => now), ct);

        // Phase 3 — hard-delete tournaments and counters past the grace
        // window. EF cascades drop participants/matches/sets/events/share
        // tokens via the relational delete behaviour.
        var tournamentsHardDeleted = await db.Tournaments
            .IgnoreQueryFilters()
            .Where(t => t.DeletedAt != null && t.DeletedAt < hardDeleteCutoff)
            .ExecuteDeleteAsync(ct);

        var countersHardDeleted = await db.Counters
            .IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && c.DeletedAt < hardDeleteCutoff)
            .ExecuteDeleteAsync(ct);

        if (countersSoftDeleted > 0 || countersHardDeleted > 0
            || tournamentsSoftDeleted > 0 || tournamentsHardDeleted > 0)
        {
            logger.LogInformation(
                "Cleanup: tournaments soft={TSoft}/hard={THard}, counters soft={CSoft}/hard={CHard}.",
                tournamentsSoftDeleted, tournamentsHardDeleted,
                countersSoftDeleted, countersHardDeleted);
        }
    }
}
