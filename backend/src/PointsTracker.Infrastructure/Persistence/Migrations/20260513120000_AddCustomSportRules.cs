using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomSportRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "custom_points_per_set",
                table: "counters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "custom_last_set_points",
                table: "counters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "custom_sets_to_win",
                table: "counters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "custom_total_sets",
                table: "counters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "custom_win_by_two",
                table: "counters",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "custom_points_per_set", table: "counters");
            migrationBuilder.DropColumn(name: "custom_last_set_points", table: "counters");
            migrationBuilder.DropColumn(name: "custom_sets_to_win", table: "counters");
            migrationBuilder.DropColumn(name: "custom_total_sets", table: "counters");
            migrationBuilder.DropColumn(name: "custom_win_by_two", table: "counters");
        }
    }
}
