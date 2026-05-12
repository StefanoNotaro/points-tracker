using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class ShareTokenRepository(AppDbContext db) : IShareTokenRepository
{
    public Task<ShareToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        db.ShareTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task AddAsync(ShareToken shareToken, CancellationToken ct = default) =>
        db.ShareTokens.Add(shareToken);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
