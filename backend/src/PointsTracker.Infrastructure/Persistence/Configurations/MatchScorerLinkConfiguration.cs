using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class MatchScorerLinkConfiguration : IEntityTypeConfiguration<MatchScorerLink>
{
    public void Configure(EntityTypeBuilder<MatchScorerLink> builder)
    {
        builder.ToTable("match_scorer_links");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.TournamentId).HasColumnName("tournament_id");
        builder.Property(l => l.MatchId).HasColumnName("match_id");
        builder.Property(l => l.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(l => l.GrantedToUserId).HasColumnName("granted_to_user_id");
        builder.Property(l => l.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(l => l.Label).HasColumnName("label").HasMaxLength(100);
        builder.Property(l => l.RevokedAt).HasColumnName("revoked_at");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(l => l.TokenHash).IsUnique();
        builder.HasIndex(l => l.MatchId).HasFilter("revoked_at IS NULL");

        // Cascade from match: when a match (and therefore its tournament) is deleted,
        // all associated scorer links are removed automatically.
        builder.HasOne<TournamentMatch>()
            .WithMany()
            .HasForeignKey(l => l.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
