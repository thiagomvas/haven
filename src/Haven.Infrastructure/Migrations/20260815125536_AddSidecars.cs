using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSidecars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sidecars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    health = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_deployed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_config = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sidecars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sidecar_networks",
                columns: table => new
                {
                    sidecar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sidecar_networks", x => new { x.sidecar_id, x.network_id });
                    table.ForeignKey(
                        name: "FK_sidecar_networks_networks_network_id",
                        column: x => x.network_id,
                        principalTable: "networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sidecar_networks_sidecars_sidecar_id",
                        column: x => x.sidecar_id,
                        principalTable: "sidecars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sidecar_networks_network_id",
                table: "sidecar_networks",
                column: "network_id");

            migrationBuilder.CreateIndex(
                name: "IX_sidecars_name",
                table: "sidecars",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sidecar_networks");

            migrationBuilder.DropTable(
                name: "sidecars");
        }
    }
}
