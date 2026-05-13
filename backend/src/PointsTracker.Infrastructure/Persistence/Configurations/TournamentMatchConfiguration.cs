using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class TournamentMatchConfiguration : IEntityTypeConfiguration<TournamentMatch>
{
    public void Configure(EntityTypeBuilder<TournamentMatch> builder)
    {
        builder.ToTable("tournament_matches");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.TournamentId).HasColumnName("tournament_id");
        builder.Property(m => m.BracketSide)
            .HasColumnName("bracket_side")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<BracketSide>(v, true));
        builder.Property(m => m.RoundNumber).HasColumnName("round_number");
        builder.Property(m => m.MatchNumber).HasColumnName("match_number");
        builder.Property(m => m.GroupNumber).HasColumnName("group_number");
        builder.Property(m => m.ParticipantAId).HasColumnName("participant_a_id");
        builder.Property(m => m.ParticipantBId).HasColumnName("participant_b_id");
        builder.Property(m => m.CounterId).HasColumnName("counter_id");
        builder.Property(m => m.WinnerParticipantId).HasColumnName("winner_participant_id");
        builder.Property(m => m.LoserParticipantId).HasColumnName("loser_participant_id");
        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<TournamentMatchStatus>(v, true));
        builder.Property(m => m.NextMatchId).HasColumnName("next_match_id");
        builder.Property(m => m.NextLoserMatchId).HasColumnName("next_loser_match_id");
        builder.Property(m => m.WinnerToSideA).HasColumnName("winner_to_side_a").HasDefaultValue(true);
        builder.Property(m => m.LoserToSideA).HasColumnName("loser_to_side_a").HasDefaultValue(true);
        builder.Property(m => m.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(m => m.IsFutureMatch);

        builder.HasIndex(m => m.TournamentId);
        builder.HasIndex(m => m.CounterId).HasFilter("counter_id IS NOT NULL");
    }
}
