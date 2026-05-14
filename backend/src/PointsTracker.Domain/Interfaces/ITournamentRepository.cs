using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Interfaces;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tournament> AddAsync(Tournament tournament, CancellationToken ct = default);
    Task<IReadOnlyList<Tournament>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Tournament>> ListBySessionTokenHashesAsync(IReadOnlyCollection<string> hashes, CancellationToken ct = default);
    Task<Tournament?> GetActiveAnonymousAsync(string sessionTokenHash, CancellationToken ct = default);
    Task<Tournament?> GetByLinkedCounterAsync(Guid counterId, CancellationToken ct = default);

    /// <summary>
    /// Force the change-tracker to treat each match as a brand-new row.
    /// Required when a generated bracket is attached to an already-tracked
    /// Tournament: EF would otherwise see non-default Ids on the navigation
    /// items and assume they're existing rows being re-attached, then issue
    /// UPDATEs (which affect 0 rows and throw DbUpdateConcurrencyException).
    /// </summary>
    void TrackAsNew(IEnumerable<TournamentMatch> matches);

    Task SaveChangesAsync(CancellationToken ct = default);
}
