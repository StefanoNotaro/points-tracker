using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class RoleAuditLogConfiguration : IEntityTypeConfiguration<RoleAuditLog>
{
    public void Configure(EntityTypeBuilder<RoleAuditLog> builder)
    {
        builder.ToTable("role_audit_log");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");

        builder.Property(e => e.FromRole)
            .HasColumnName("from_role")
            .HasMaxLength(50)
            .HasConversion(
                v => v == null ? null : v.Value.ToWireString(),
                v => v == null ? (GlobalRole?)null : GlobalRoleExtensions.ParseGlobalRole(v));

        builder.Property(e => e.ToRole)
            .HasColumnName("to_role")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToWireString(), v => GlobalRoleExtensions.ParseGlobalRole(v));

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToWireString(), v => RoleAuditEventTypeExtensions.ParseRoleAuditEventType(v));

        builder.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1024);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
            .HasDatabaseName("ix_role_audit_log_user_occurred")
            .IsDescending(false, true);
    }
}
