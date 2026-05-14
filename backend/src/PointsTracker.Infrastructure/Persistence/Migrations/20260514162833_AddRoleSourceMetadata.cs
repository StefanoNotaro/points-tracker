using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleSourceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: every existing user's role is treated as the platform default
            // (no IdP claim sync existed before this migration). NOW() captures the
            // moment of migration; "system" records that no human actor set the role.
            migrationBuilder.AddColumn<string>(
                name: "role_source",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<System.DateTime>(
                name: "role_updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "role_updated_by",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.CreateIndex(
                name: "IX_users_role",
                table: "users",
                column: "role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_source",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_updated_by",
                table: "users");
        }
    }
}
