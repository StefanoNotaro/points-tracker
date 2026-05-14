using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class TournamentRepository(AppDbContext db) : ITournamentRepository
{
    public void TrackAsNew(IEnumerable<TournamentMatch> matches)
    {
        foreach (var m in matches)
            db.Entry(m).State = EntityState.Added;
    }


    public Task<Tournament?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tournament> AddAsync(Tournament tournament, CancellationToken ct = default)
    {
        db.Tournaments.Add(tournament);
        return Task.FromResult(tournament);
    }

    public async Task<IReadOnlyList<Tournament>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
        await db.Tournaments
            .Include(t => t.Participants)
            .Where(t => t.OwnerUserId == ownerUserId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Tournament>> ListBySessionTokenHashesAsync(
        IReadOnlyCollection<string> hashes, CancellationToken ct = default)
    {
        if (hashes.Count == 0) return [];
        return await db.Tournaments
            .Include(t => t.Participants)
            .Where(t => t.SessionTokenHash != null && hashes.Contains(t.SessionTokenHash))
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);
    }

    public Task<Tournament?> GetActiveAnonymousAsync(string sessionTokenHash, CancellationToken ct = default) =>
        db.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Matches)
            .Where(t => t.SessionTokenHash == sessionTokenHash
                        && t.Status != TournamentStatus.Completed
                        && t.Status != TournamentStatus.Abandoned)
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<Tournament?> GetByLinkedCounterAsync(Guid counterId, CancellationToken ct = default) =>
        db.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Matches.Any(m => m.CounterId == counterId), ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
