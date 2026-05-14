using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class CleanupAuditLogConfiguration : IEntityTypeConfiguration<CleanupAuditLog>
{
    public void Configure(EntityTypeBuilder<CleanupAuditLog> builder)
    {
        builder.ToTable("cleanup_audit_log");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(64)
            .IsRequired()
            .HasConversion(v => v.ToWireString(), v => CleanupActionExtensions.ParseCleanupAction(v));

        builder.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(255).IsRequired();
        builder.Property(e => e.TargetCount).HasColumnName("target_count").IsRequired();
        builder.Property(e => e.TargetIdsJson).HasColumnName("target_ids").HasColumnType("jsonb");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1024);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("ix_cleanup_audit_log_occurred")
            .IsDescending(true);
    }
}
