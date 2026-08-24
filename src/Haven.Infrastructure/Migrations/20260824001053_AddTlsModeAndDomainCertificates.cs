using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTlsModeAndDomainCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tls_mode",
                table: "service_registry_domains",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: the old boolean's only behavior was ACME (TlsMode.Acme = 1), before
            // "Custom" (bring-your-own-certificate) existed.
            migrationBuilder.Sql(
                "UPDATE service_registry_domains SET tls_mode = 1 WHERE enable_tls = true;");

            migrationBuilder.DropColumn(
                name: "enable_tls",
                table: "service_registry_domains");

            migrationBuilder.CreateTable(
                name: "domain_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_registry_domain_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_domain_certificates", x => x.id);
                    table.ForeignKey(
                        name: "FK_domain_certificates_service_registry_domains_service_regist~",
                        column: x => x.service_registry_domain_id,
                        principalTable: "service_registry_domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_domain_certificates_service_registry_domain_id",
                table: "domain_certificates",
                column: "service_registry_domain_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domain_certificates");

            migrationBuilder.AddColumn<bool>(
                name: "enable_tls",
                table: "service_registry_domains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE service_registry_domains SET enable_tls = (tls_mode = 1);");

            migrationBuilder.DropColumn(
                name: "tls_mode",
                table: "service_registry_domains");
        }
    }
}
