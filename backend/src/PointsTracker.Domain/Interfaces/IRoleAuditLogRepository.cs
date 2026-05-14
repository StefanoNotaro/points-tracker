using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface IRoleAuditLogRepository
{
    Task AddAsync(RoleAuditLog entry, CancellationToken ct = default);

    Task<IReadOnlyList<RoleAuditLog>> GetForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken ct = default);
}
