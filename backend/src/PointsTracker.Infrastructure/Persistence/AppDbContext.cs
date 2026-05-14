using Microsoft.EntityFrameworkCore;
using PointsTracker.Domain.Entities;
using PointsTracker.Infrastructure.Persistence.Configurations;

namespace PointsTracker.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<CounterSet> CounterSets => Set<CounterSet>();
    public DbSet<CounterEvent> CounterEvents => Set<CounterEvent>();
    public DbSet<ShareToken> ShareTokens => Set<ShareToken>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RoleAuditLog> RoleAuditLog => Set<RoleAuditLog>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
