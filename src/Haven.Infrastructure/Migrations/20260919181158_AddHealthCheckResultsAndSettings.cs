using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthCheckResultsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "consecutive_failures",
                table: "health_checks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "consecutive_successes",
                table: "health_checks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "failure_threshold",
                table: "health_checks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "last_run_duration_ms",
                table: "health_checks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_run_message",
                table: "health_checks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_run_reason",
                table: "health_checks",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<int>(
                name: "retries",
                table: "health_checks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "success_threshold",
                table: "health_checks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "health_check_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ran_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    exit_code = table.Column<long>(type: "bigint", nullable: true),
                    output = table.Column<string>(type: "text", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_check_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_check_results_health_checks_health_check_id",
                        column: x => x.health_check_id,
                        principalTable: "health_checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_health_check_results_check_ran_at",
                table: "health_check_results",
                columns: new[] { "health_check_id", "ran_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_check_results");

            migrationBuilder.DropColumn(
                name: "consecutive_failures",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "consecutive_successes",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "failure_threshold",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "last_run_duration_ms",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "last_run_message",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "last_run_reason",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "retries",
                table: "health_checks");

            migrationBuilder.DropColumn(
                name: "success_threshold",
                table: "health_checks");
        }
    }
}
