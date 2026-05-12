using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class CounterSetConfiguration : IEntityTypeConfiguration<CounterSet>
{
    public void Configure(EntityTypeBuilder<CounterSet> builder)
    {
        builder.ToTable("counter_sets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CounterId).HasColumnName("counter_id");
        builder.Property(s => s.SetNumber).HasColumnName("set_number");
        builder.Property(s => s.ScoreA).HasColumnName("score_a");
        builder.Property(s => s.ScoreB).HasColumnName("score_b");
        builder.Property(s => s.Winner)
            .HasColumnName("winner")
            .HasConversion(v => v.HasValue ? v.Value.ToString() : null,
                v => v != null ? Enum.Parse<Team>(v) : null);
        builder.Property(s => s.StartedAt).HasColumnName("started_at");
        builder.Property(s => s.EndedAt).HasColumnName("ended_at");

        builder.HasIndex(s => s.CounterId);
    }
}
