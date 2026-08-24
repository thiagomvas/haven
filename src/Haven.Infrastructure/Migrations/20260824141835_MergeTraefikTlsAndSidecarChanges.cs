using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeTraefikTlsAndSidecarChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_registry_services_service_id",
                table: "service_registry");

            migrationBuilder.AddColumn<string>(
                name: "internal_base_path",
                table: "service_registry_domains",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ssl_certificate_id",
                table: "service_registry_domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tls_mode",
                table: "service_registry_domains",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateTable(
                name: "ssl_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    certificate_pem = table.Column<string>(type: "text", nullable: false),
                    private_key_pem = table.Column<string>(type: "text", nullable: false),
                    not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    subject_common_name = table.Column<string>(type: "text", nullable: true),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssl_certificates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_domains_ssl_certificate_id",
                table: "service_registry_domains",
                column: "ssl_certificate_id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_service_registry_domains_ssl_certificates_ssl_certificate_id",
                table: "service_registry_domains",
                column: "ssl_certificate_id",
                principalTable: "ssl_certificates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.DropForeignKey(
                name: "FK_service_registry_domains_ssl_certificates_ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.DropTable(
                name: "ssl_certificates");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_domains_ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_sidecar_id",
                table: "service_registry");

            migrationBuilder.DropCheckConstraint(
                name: "CK_service_registry_owner",
                table: "service_registry");

            migrationBuilder.DropColumn(
                name: "internal_base_path",
                table: "service_registry_domains");

            migrationBuilder.DropColumn(
                name: "ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.DropColumn(
                name: "tls_mode",
                table: "service_registry_domains");

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
