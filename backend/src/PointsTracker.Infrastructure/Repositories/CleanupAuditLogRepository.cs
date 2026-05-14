using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class CleanupAuditLogRepository(AppDbContext db) : ICleanupAuditLogRepository
{
    public Task AddAsync(CleanupAuditLog entry, CancellationToken ct = default)
    {
        db.CleanupAuditLog.Add(entry);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CleanupAuditLog>> GetRecentAsync(int take = 100, CancellationToken ct = default) =>
        await db.CleanupAuditLog
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
