using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class MatchScorerLinkRepository(AppDbContext db) : IMatchScorerLinkRepository
{
    public Task<MatchScorerLink?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.MatchScorerLinks.FirstOrDefaultAsync(l => l.TokenHash == tokenHash, ct);

    public Task<MatchScorerLink?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.MatchScorerLinks.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<MatchScorerLink>> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default) =>
        await db.MatchScorerLinks.Where(l => l.MatchId == matchId).ToListAsync(ct);

    public Task AddAsync(MatchScorerLink link, CancellationToken ct = default)
    {
        db.MatchScorerLinks.Add(link);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
