using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.ExternalId).HasColumnName("external_id").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToWireString(), v => GlobalRoleExtensions.ParseGlobalRole(v));

        builder.Property(u => u.RoleSource)
            .HasColumnName("role_source")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(v => v.ToWireString(), v => RoleSourceExtensions.ParseRoleSource(v));

        builder.Property(u => u.RoleUpdatedAt).HasColumnName("role_updated_at");
        builder.Property(u => u.RoleUpdatedBy).HasColumnName("role_updated_by").HasMaxLength(255).IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(u => u.ExternalId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Role);
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
