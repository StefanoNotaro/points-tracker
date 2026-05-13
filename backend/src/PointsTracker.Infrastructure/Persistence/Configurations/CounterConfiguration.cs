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

        builder.Property(c => c.CustomPointsPerSet).HasColumnName("custom_points_per_set");
        builder.Property(c => c.CustomLastSetPoints).HasColumnName("custom_last_set_points");
        builder.Property(c => c.CustomSetsToWin).HasColumnName("custom_sets_to_win");
        builder.Property(c => c.CustomTotalSets).HasColumnName("custom_total_sets");
        builder.Property(c => c.CustomWinByTwo).HasColumnName("custom_win_by_two");

        builder.Property(c => c.SideSwitchCount).HasColumnName("side_switch_count").HasDefaultValue(0);
        builder.Property(c => c.PendingSideSwitchConfirmation).HasColumnName("pending_side_switch_confirmation").HasDefaultValue(false);
        builder.Property(c => c.IndoorSwitchEverySets).HasColumnName("indoor_switch_every_sets");
        builder.Property(c => c.BeachAutoSwitchSides).HasColumnName("beach_auto_switch_sides").HasDefaultValue(true);
        builder.Property(c => c.CustomTimeoutsPerSet).HasColumnName("custom_timeouts_per_set");
        builder.Property(c => c.CustomTimeoutDurationSeconds).HasColumnName("custom_timeout_duration_seconds");

        // Computed read-only properties. EF Core 9 may otherwise try to interpret
        // CurrentSet as a single-cardinality navigation (and create a shadow FK),
        // or interpret EffectiveRules as a complex/owned type, both of which break
        // SaveChanges with phantom UPDATE columns and concurrency exceptions.
        builder.Ignore(c => c.EffectiveRules);
        builder.Ignore(c => c.CurrentSet);
        builder.Ignore(c => c.CurrentSetNumber);
        builder.Ignore(c => c.CurrentScoreA);
        builder.Ignore(c => c.CurrentScoreB);
        builder.Ignore(c => c.SetsWonA);
        builder.Ignore(c => c.SetsWonB);

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
