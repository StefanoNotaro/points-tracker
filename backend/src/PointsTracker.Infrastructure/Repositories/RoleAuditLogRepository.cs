using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class RoleAuditLogRepository(AppDbContext db) : IRoleAuditLogRepository
{
    public Task AddAsync(RoleAuditLog entry, CancellationToken ct = default)
    {
        db.RoleAuditLog.Add(entry);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RoleAuditLog>> GetForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken ct = default) =>
        await db.RoleAuditLog
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .ToListAsync(ct);
}
