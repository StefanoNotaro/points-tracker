using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cleanup_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    target_count = table.Column<int>(type: "integer", nullable: false),
                    target_ids = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cleanup_audit_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cleanup_audit_log_occurred",
                table: "cleanup_audit_log",
                column: "occurred_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cleanup_audit_log");
        }
    }
}
