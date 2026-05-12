using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Persistence;

namespace PointsTracker.Infrastructure.Repositories;

public class CounterRepository(AppDbContext db) : ICounterRepository
{
    public Task<Counter?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Counters
            .Include(c => c.Sets)
            .Include(c => c.Events)
            .Include(c => c.ShareTokens)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Counter> AddAsync(Counter counter, CancellationToken ct = default)
    {
        db.Counters.Add(counter);
        return counter;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
