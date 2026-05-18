using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface IMatchScorerLinkRepository
{
    Task<MatchScorerLink?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<MatchScorerLink?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MatchScorerLink>> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default);
    Task AddAsync(MatchScorerLink link, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
