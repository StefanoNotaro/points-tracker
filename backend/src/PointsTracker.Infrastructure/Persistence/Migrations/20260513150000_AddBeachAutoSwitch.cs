using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBeachAutoSwitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "beach_auto_switch_sides",
                table: "counters",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "beach_auto_switch_sides", table: "counters");
        }
    }
}
