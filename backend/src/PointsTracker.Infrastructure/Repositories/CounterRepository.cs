using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class CounterRepository(AppDbContext db) : ICounterRepository
{
    public Task<Counter?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Counters
            // Sort sets so Counter.CurrentSet (== _sets.Last()) is always the latest
            // set, regardless of how the DB returns rows.
            .Include(c => c.Sets.OrderBy(s => s.SetNumber))
            .Include(c => c.Events.OrderBy(e => e.CreatedAt))
            .Include(c => c.ShareTokens)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Counter> AddAsync(Counter counter, CancellationToken ct = default)
    {
        db.Counters.Add(counter);
        return counter;
    }

    public async Task<IReadOnlyList<Counter>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
        await db.Counters
            .Include(c => c.Sets)
            .Where(c => c.OwnerUserId == ownerUserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Counter>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await db.Counters
            .Include(c => c.Sets)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Counter>> ListByTournamentAsync(Guid tournamentId, CancellationToken ct = default) =>
        await db.Counters
            .Where(c => c.LinkedTournamentId == tournamentId)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
