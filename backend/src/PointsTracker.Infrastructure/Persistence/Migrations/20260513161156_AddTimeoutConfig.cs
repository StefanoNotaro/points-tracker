using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeoutConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "custom_timeout_duration_seconds",
                table: "counters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "custom_timeouts_per_set",
                table: "counters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_timeout_duration_seconds",
                table: "counters");

            migrationBuilder.DropColumn(
                name: "custom_timeouts_per_set",
                table: "counters");
        }
    }
}
