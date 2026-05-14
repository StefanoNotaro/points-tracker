using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_audit_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_audit_log_user_occurred",
                table: "role_audit_log",
                columns: new[] { "user_id", "occurred_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_audit_log");
        }
    }
}
