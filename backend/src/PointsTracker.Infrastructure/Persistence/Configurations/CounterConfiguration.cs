using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class CounterConfiguration : IEntityTypeConfiguration<Counter>
{
    public void Configure(EntityTypeBuilder<Counter> builder)
    {
        builder.ToTable("counters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.SportType)
            .HasColumnName("sport_type")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<SportType>(v, true));
        builder.Property(c => c.OwnerUserId).HasColumnName("owner_user_id");
        builder.Property(c => c.SessionTokenHash).HasColumnName("session_token_hash").HasMaxLength(64);
        builder.Property(c => c.TeamAName).HasColumnName("team_a_name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.TeamBName).HasColumnName("team_b_name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<CounterStatus>(v, true));
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(c => c.DeletedAt == null);

        builder.HasMany(c => c.Sets)
            .WithOne()
            .HasForeignKey(s => s.CounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Events)
            .WithOne()
            .HasForeignKey(e => e.CounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ShareTokens)
            .WithOne()
            .HasForeignKey(t => t.CounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.OwnerUserId).HasFilter("deleted_at IS NULL");
    }
}
