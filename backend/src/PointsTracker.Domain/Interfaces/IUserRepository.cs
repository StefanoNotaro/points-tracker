using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task<int> CountActiveSuperAdminsAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
