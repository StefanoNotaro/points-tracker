using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface ICounterRepository
{
    Task<Counter?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Counter> AddAsync(Counter counter, CancellationToken ct = default);
    Task<IReadOnlyList<Counter>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Counter>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Counter>> ListByTournamentAsync(Guid tournamentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
