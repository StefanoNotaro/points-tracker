using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Infrastructure.Services;

/// <summary>
/// Periodic background sweep that delegates to <see cref="ICleanupService"/>
/// — the same code path the admin dashboard's "Run policy now" button uses.
/// Writes a single <c>cleanup_audit_log</c> row per pass with actor
/// <c>system:background</c>.
///
/// See docs/ADMIN_CLEANUP.md for the retention windows.
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
        using var scope = scopeFactory.CreateScope();
        var cleanup = scope.ServiceProvider.GetRequiredService<ICleanupService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<ICleanupAuditLogRepository>();

        var result = await cleanup.RunPolicyAsync(ct);
        var total = result.CountersSoftDeleted + result.TournamentsSoftDeleted
                    + result.CountersHardPurged + result.TournamentsHardPurged
                    + result.ShareTokensPurged;

        if (total > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(
                    CleanupAction.RunPolicy,
                    actor: "system:background",
                    targetCount: total,
                    targetIds: null,
                    reason: $"counters soft={result.CountersSoftDeleted}/hard={result.CountersHardPurged}, "
                            + $"tournaments soft={result.TournamentsSoftDeleted}/hard={result.TournamentsHardPurged}, "
                            + $"tokens purged={result.ShareTokensPurged}"),
                ct);
            await auditRepo.SaveChangesAsync(ct);

            logger.LogInformation(
                "Cleanup: tournaments soft={TSoft}/hard={THard}, counters soft={CSoft}/hard={CHard}, tokens={Tokens}.",
                result.TournamentsSoftDeleted, result.TournamentsHardPurged,
                result.CountersSoftDeleted, result.CountersHardPurged,
                result.ShareTokensPurged);
        }
    }
}
