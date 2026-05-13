using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Services;

/// <summary>
/// Periodically removes orphaned anonymous counters. Authenticated users'
/// counters are out of scope — they manage their own data through the UI.
///
/// The cleanup runs in two phases:
///   1. Soft-delete anonymous counters whose UpdatedAt is older than
///      <see cref="CounterCleanupOptions.AnonymousInactiveDays"/>. Soft-deleted
///      rows disappear from the API thanks to the global query filter.
///   2. Hard-delete any soft-deleted row whose DeletedAt is older than
///      <see cref="CounterCleanupOptions.HardDeleteGraceDays"/>. EF cascades
///      take care of the related sets, events and share tokens.
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

        // Brief startup delay so we don't fight EF migrations on the very first
        // tick after boot.
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
                // Never let an exception kill the worker — we'll try again on
                // the next tick.
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

        // Phase 1 — soft-delete stale anonymous counters. The global query
        // filter hides already soft-deleted rows so this only touches live
        // ones. We bypass the filter for safety using IgnoreQueryFilters
        // and re-check DeletedAt == null in the WHERE clause.
        var softDeleted = await db.Counters
            .IgnoreQueryFilters()
            .Where(c => c.DeletedAt == null
                        && c.OwnerUserId == null
                        && c.UpdatedAt < softDeleteCutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeletedAt, _ => now)
                .SetProperty(c => c.UpdatedAt, _ => now), ct);

        // Phase 2 — hard-delete any counter (anonymous or otherwise) that has
        // been sitting in the soft-deleted state past the grace window. EF
        // cascades drop sets / events / share tokens via the relational
        // delete behavior configured in CounterConfiguration.
        var hardDeleted = await db.Counters
            .IgnoreQueryFilters()
            .Where(c => c.DeletedAt != null && c.DeletedAt < hardDeleteCutoff)
            .ExecuteDeleteAsync(ct);

        if (softDeleted > 0 || hardDeleted > 0)
        {
            logger.LogInformation(
                "Counter cleanup: soft-deleted {SoftDeleted}, hard-deleted {HardDeleted}.",
                softDeleted, hardDeleted);
        }
    }
}
