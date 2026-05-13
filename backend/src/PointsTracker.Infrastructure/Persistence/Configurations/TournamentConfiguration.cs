using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.ToTable("tournaments");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.SportType)
            .HasColumnName("sport_type")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<SportType>(v, true));
        builder.Property(t => t.Format)
            .HasColumnName("format")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<TournamentFormat>(v, true));
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<TournamentStatus>(v, true));

        builder.Property(t => t.OwnerUserId).HasColumnName("owner_user_id");
        builder.Property(t => t.SessionTokenHash).HasColumnName("session_token_hash").HasMaxLength(64);

        builder.Property(t => t.CustomPointsPerSet).HasColumnName("custom_points_per_set");
        builder.Property(t => t.CustomLastSetPoints).HasColumnName("custom_last_set_points");
        builder.Property(t => t.CustomSetsToWin).HasColumnName("custom_sets_to_win");
        builder.Property(t => t.CustomTotalSets).HasColumnName("custom_total_sets");
        builder.Property(t => t.CustomWinByTwo).HasColumnName("custom_win_by_two");
        builder.Property(t => t.IndoorSwitchEverySets).HasColumnName("indoor_switch_every_sets");
        builder.Property(t => t.BeachAutoSwitchSides).HasColumnName("beach_auto_switch_sides").HasDefaultValue(true);
        builder.Property(t => t.CustomTimeoutsPerSet).HasColumnName("custom_timeouts_per_set");
        builder.Property(t => t.CustomTimeoutDurationSeconds).HasColumnName("custom_timeout_duration_seconds");
        builder.Property(t => t.GroupCount).HasColumnName("group_count");
        builder.Property(t => t.AdvancePerGroup).HasColumnName("advance_per_group");

        builder.Property(t => t.FinalPointsPerSet).HasColumnName("final_points_per_set");
        builder.Property(t => t.FinalLastSetPoints).HasColumnName("final_last_set_points");
        builder.Property(t => t.FinalSetsToWin).HasColumnName("final_sets_to_win");
        builder.Property(t => t.FinalTotalSets).HasColumnName("final_total_sets");
        builder.Property(t => t.FinalWinByTwo).HasColumnName("final_win_by_two");
        builder.Property(t => t.FinalTimeoutsPerSet).HasColumnName("final_timeouts_per_set");
        builder.Property(t => t.FinalTimeoutDurationSeconds).HasColumnName("final_timeout_duration_seconds");

        builder.Property(t => t.SemifinalPointsPerSet).HasColumnName("semifinal_points_per_set");
        builder.Property(t => t.SemifinalLastSetPoints).HasColumnName("semifinal_last_set_points");
        builder.Property(t => t.SemifinalSetsToWin).HasColumnName("semifinal_sets_to_win");
        builder.Property(t => t.SemifinalTotalSets).HasColumnName("semifinal_total_sets");
        builder.Property(t => t.SemifinalWinByTwo).HasColumnName("semifinal_win_by_two");
        builder.Property(t => t.SemifinalTimeoutsPerSet).HasColumnName("semifinal_timeouts_per_set");
        builder.Property(t => t.SemifinalTimeoutDurationSeconds).HasColumnName("semifinal_timeout_duration_seconds");

        builder.Property(t => t.StartsAt).HasColumnName("starts_at");
        builder.Property(t => t.EndsAt).HasColumnName("ends_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

        builder.Ignore(t => t.CurrentCustomRules);

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.HasMany(t => t.Participants)
            .WithOne()
            .HasForeignKey(p => p.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Matches)
            .WithOne()
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.OwnerUserId).HasFilter("deleted_at IS NULL");
        builder.HasIndex(t => t.SessionTokenHash).HasFilter("deleted_at IS NULL");
    }
}
