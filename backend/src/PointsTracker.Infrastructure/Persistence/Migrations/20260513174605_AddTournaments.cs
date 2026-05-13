using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sport_type = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    custom_points_per_set = table.Column<int>(type: "integer", nullable: true),
                    custom_last_set_points = table.Column<int>(type: "integer", nullable: true),
                    custom_sets_to_win = table.Column<int>(type: "integer", nullable: true),
                    custom_total_sets = table.Column<int>(type: "integer", nullable: true),
                    custom_win_by_two = table.Column<bool>(type: "boolean", nullable: true),
                    indoor_switch_every_sets = table.Column<int>(type: "integer", nullable: true),
                    beach_auto_switch_sides = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    custom_timeouts_per_set = table.Column<int>(type: "integer", nullable: true),
                    custom_timeout_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tournament_matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bracket_side = table.Column<string>(type: "text", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    match_number = table.Column<int>(type: "integer", nullable: false),
                    participant_a_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_b_id = table.Column<Guid>(type: "uuid", nullable: true),
                    counter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winner_participant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loser_participant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    next_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    next_loser_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winner_to_side_a = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    loser_to_side_a = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_matches_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seed = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_participants", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_participants_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_counter_id",
                table: "tournament_matches",
                column: "counter_id",
                filter: "counter_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_tournament_id",
                table: "tournament_matches",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_participants_tournament_id",
                table: "tournament_participants",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_participants_tournament_id_team_name",
                table: "tournament_participants",
                columns: new[] { "tournament_id", "team_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_owner_user_id",
                table: "tournaments",
                column: "owner_user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_session_token_hash",
                table: "tournaments",
                column: "session_token_hash",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_matches");

            migrationBuilder.DropTable(
                name: "tournament_participants");

            migrationBuilder.DropTable(
                name: "tournaments");
        }
    }
}
