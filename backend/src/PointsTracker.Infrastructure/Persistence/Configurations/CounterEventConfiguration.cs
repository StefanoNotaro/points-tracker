using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class CounterEventConfiguration : IEntityTypeConfiguration<CounterEvent>
{
    public void Configure(EntityTypeBuilder<CounterEvent> builder)
    {
        builder.ToTable("counter_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CounterId).HasColumnName("counter_id");
        builder.Property(e => e.SetNumber).HasColumnName("set_number");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Team)
            .HasColumnName("team")
            .HasConversion(v => v.ToString(), v => Enum.Parse<Team>(v));
        builder.Property(e => e.ScoreABefore).HasColumnName("score_a_before");
        builder.Property(e => e.ScoreBBefore).HasColumnName("score_b_before");
        builder.Property(e => e.ScoreAAfter).HasColumnName("score_a_after");
        builder.Property(e => e.ScoreBAfter).HasColumnName("score_b_after");
        builder.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(e => new { e.CounterId, e.CreatedAt });
    }
}
