using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSidecarDomainSupportAndTraefikDashboardAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_registry_services_service_id",
                table: "service_registry");

            migrationBuilder.AlterColumn<Guid>(
                name: "service_id",
                table: "service_registry",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "sidecar_id",
                table: "service_registry",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry",
                column: "sidecar_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_service_registry_owner",
                table: "service_registry",
                sql: "(service_id IS NOT NULL AND sidecar_id IS NULL) OR (service_id IS NULL AND sidecar_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_service_registry_services_service_id",
                table: "service_registry",
                column: "service_id",
                principalTable: "services",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_service_registry_sidecars_sidecar_id",
                table: "service_registry",
                column: "sidecar_id",
                principalTable: "sidecars",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_registry_services_service_id",
                table: "service_registry");

            migrationBuilder.DropForeignKey(
                name: "FK_service_registry_sidecars_sidecar_id",
                table: "service_registry");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry");

            migrationBuilder.DropCheckConstraint(
                name: "CK_service_registry_owner",
                table: "service_registry");

            migrationBuilder.DropColumn(
                name: "sidecar_id",
                table: "service_registry");

            migrationBuilder.AlterColumn<Guid>(
                name: "service_id",
                table: "service_registry",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_service_registry_services_service_id",
                table: "service_registry",
                column: "service_id",
                principalTable: "services",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
