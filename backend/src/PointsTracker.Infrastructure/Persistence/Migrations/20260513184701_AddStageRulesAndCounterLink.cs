using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageRulesAndCounterLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "final_last_set_points",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_points_per_set",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_sets_to_win",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_timeout_duration_seconds",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_timeouts_per_set",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_total_sets",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "final_win_by_two",
                table: "tournaments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_last_set_points",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_points_per_set",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_sets_to_win",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_timeout_duration_seconds",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_timeouts_per_set",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "semifinal_total_sets",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "semifinal_win_by_two",
                table: "tournaments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_tournament_id",
                table: "counters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_tournament_match_id",
                table: "counters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linked_tournament_name",
                table: "counters",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_counters_linked_tournament_id",
                table: "counters",
                column: "linked_tournament_id",
                filter: "linked_tournament_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_counters_linked_tournament_id",
                table: "counters");

            migrationBuilder.DropColumn(
                name: "final_last_set_points",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_points_per_set",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_sets_to_win",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_timeout_duration_seconds",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_timeouts_per_set",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_total_sets",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "final_win_by_two",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_last_set_points",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_points_per_set",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_sets_to_win",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_timeout_duration_seconds",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_timeouts_per_set",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_total_sets",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "semifinal_win_by_two",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "linked_tournament_id",
                table: "counters");

            migrationBuilder.DropColumn(
                name: "linked_tournament_match_id",
                table: "counters");

            migrationBuilder.DropColumn(
                name: "linked_tournament_name",
                table: "counters");
        }
    }
}
