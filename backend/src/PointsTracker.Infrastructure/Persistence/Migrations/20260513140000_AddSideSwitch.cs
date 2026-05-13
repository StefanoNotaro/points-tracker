using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSideSwitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "side_switch_count",
                table: "counters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "pending_side_switch_confirmation",
                table: "counters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "indoor_switch_every_sets",
                table: "counters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "side_switch_count", table: "counters");
            migrationBuilder.DropColumn(name: "pending_side_switch_confirmation", table: "counters");
            migrationBuilder.DropColumn(name: "indoor_switch_every_sets", table: "counters");
        }
    }
}
