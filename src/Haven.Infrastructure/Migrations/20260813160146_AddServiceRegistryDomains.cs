using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRegistryDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_registry_domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_registry_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hostname = table.Column<string>(type: "text", nullable: false),
                    container_port = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_registry_domains", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_registry_domains_service_registry_service_registry_~",
                        column: x => x.service_registry_entry_id,
                        principalTable: "service_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_domains_hostname",
                table: "service_registry_domains",
                column: "hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_domains_service_registry_entry_id",
                table: "service_registry_domains",
                column: "service_registry_entry_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_registry_domains");
        }
    }
}