using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSslCertificateLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domain_certificates");

            migrationBuilder.AddColumn<Guid>(
                name: "ssl_certificate_id",
                table: "service_registry_domains",
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
                name: "FK_service_registry_domains_ssl_certificates_ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.DropTable(
                name: "ssl_certificates");

            migrationBuilder.DropIndex(
                name: "IX_service_registry_domains_ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.DropColumn(
                name: "ssl_certificate_id",
                table: "service_registry_domains");

            migrationBuilder.CreateTable(
                name: "domain_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_registry_domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_pem = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    private_key_pem = table.Column<string>(type: "text", nullable: false),
                    subject_common_name = table.Column<string>(type: "text", nullable: true),
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
    }
}
