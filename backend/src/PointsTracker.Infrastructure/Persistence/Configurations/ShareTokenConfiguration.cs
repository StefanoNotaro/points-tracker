using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class ShareTokenConfiguration : IEntityTypeConfiguration<ShareToken>
{
    public void Configure(EntityTypeBuilder<ShareToken> builder)
    {
        builder.ToTable("share_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.CounterId).HasColumnName("counter_id");
        builder.Property(t => t.Token).HasColumnName("token").HasMaxLength(512).IsRequired();
        builder.Property(t => t.Scope)
            .HasColumnName("scope")
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<ShareScope>(v, true));
        builder.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.CounterId).HasFilter("revoked_at IS NULL");
    }
}
