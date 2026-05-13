using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> builder)
    {
        builder.ToTable("tournament_participants");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TournamentId).HasColumnName("tournament_id");
        builder.Property(p => p.TeamName).HasColumnName("team_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Seed).HasColumnName("seed");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.RegisteredAt).HasColumnName("registered_at");

        builder.HasIndex(p => p.TournamentId);
        builder.HasIndex(p => new { p.TournamentId, p.TeamName }).IsUnique();
    }
}
