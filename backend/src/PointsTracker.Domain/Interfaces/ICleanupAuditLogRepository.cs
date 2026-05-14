using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface ICleanupAuditLogRepository
{
    Task AddAsync(CleanupAuditLog entry, CancellationToken ct = default);

    Task<IReadOnlyList<CleanupAuditLog>> GetRecentAsync(int take = 100, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
