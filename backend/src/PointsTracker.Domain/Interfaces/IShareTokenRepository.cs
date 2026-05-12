using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface IShareTokenRepository
{
    Task<ShareToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(ShareToken shareToken, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
