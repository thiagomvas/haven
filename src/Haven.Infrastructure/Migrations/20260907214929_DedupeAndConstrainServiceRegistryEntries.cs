using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DedupeAndConstrainServiceRegistryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A non-atomic check-then-insert race in ServiceRegistry.EnsureServiceRegisteredAsync could
            // produce more than one row per ServiceId/SidecarId. Before making that impossible via the
            // unique indexes below, collapse any existing duplicates down to the earliest-registered row
            // (ties broken by id) - the same ordering GetForServiceAsync/GetForSidecarAsync now use.
            migrationBuilder.Sql(@"
                DELETE FROM service_registry sr
                USING service_registry keep
                WHERE sr.service_id IS NOT NULL
                  AND sr.service_id = keep.service_id
                  AND (sr.registered_at, sr.id) > (keep.registered_at, keep.id);

                DELETE FROM service_registry sr
                USING service_registry keep
                WHERE sr.sidecar_id IS NOT NULL
                  AND sr.sidecar_id = keep.sidecar_id
                  AND (sr.registered_at, sr.id) > (keep.registered_at, keep.id);
            ");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_service_id",
                table: "service_registry");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_service_id",
                table: "service_registry",
                column: "service_id",
                unique: true,
                filter: "service_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry",
                column: "sidecar_id",
                unique: true,
                filter: "sidecar_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_registry_service_id",
                table: "service_registry");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_service_id",
                table: "service_registry",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry",
                column: "sidecar_id");
        }
    }
}