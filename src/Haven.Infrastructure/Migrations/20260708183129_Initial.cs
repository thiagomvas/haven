using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_type = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "git_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_type = table.Column<string>(type: "text", nullable: false),
                    host_url = table.Column<string>(type: "text", nullable: true),
                    auth_method = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    primary_credential = table.Column<string>(type: "text", nullable: false),
                    secondary_credential = table.Column<string>(type: "text", nullable: true),
                    webhook_secret = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    access_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_git_credentials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "haven_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_haven_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_channel_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    config = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_channel_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    alias = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    require_password_change = table.Column<bool>(type: "boolean", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_rule", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_rule_notification_channel_config_channel_confi~",
                        column: x => x.channel_config_id,
                        principalTable: "notification_channel_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "environments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    alias = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    network_name = table.Column<string>(type: "text", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_environments", x => x.id);
                    table.ForeignKey(
                        name: "FK_environments_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_name });
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    event_payload = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    response = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_attempt", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_attempt_notification_rule_RuleId",
                        column: x => x.RuleId,
                        principalTable: "notification_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "networks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "TEXT", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DockerNetworkId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_networks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_networks_environments_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_networks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    exposure_mode = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastDeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    token = table.Column<string>(type: "text", nullable: false),
                    source_config = table.Column<string>(type: "TEXT", nullable: true),
                    GitCredentialId = table.Column<Guid>(type: "uuid", nullable: true),
                    GitCredentialsId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_services_environments_environment_id",
                        column: x => x.environment_id,
                        principalTable: "environments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_services_git_credentials_GitCredentialsId",
                        column: x => x.GitCredentialsId,
                        principalTable: "git_credentials",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    triggered_by = table.Column<string>(type: "text", nullable: true),
                    log_file = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deployments", x => x.id);
                    table.ForeignKey(
                        name: "FK_deployments_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "text", nullable: false),
                    value_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.id);
                    table.ForeignKey(
                        name: "FK_feature_flags_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_networks",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_networks", x => new { x.service_id, x.network_id });
                    table.ForeignKey(
                        name: "FK_service_networks_networks_network_id",
                        column: x => x.network_id,
                        principalTable: "networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_networks_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ports = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_registry", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_registry_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_volumes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    target = table.Column<string>(type: "text", nullable: false),
                    read_only = table.Column<bool>(type: "boolean", nullable: false),
                    backup_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_volumes", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_volumes_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deployments_service_id",
                table: "deployments",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_environments_project_id_alias",
                table: "environments",
                columns: new[] { "project_id", "alias" },
                unique: true,
                filter: "\"alias\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_events_event_type",
                table: "events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_events_triggered_at",
                table: "events",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_ServiceId",
                table: "feature_flags",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_haven_settings_category",
                table: "haven_settings",
                column: "category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_networks_environment_id",
                table: "networks",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "IX_networks_project_id",
                table: "networks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_attempt_RuleId",
                table: "notification_attempt",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_rule_channel_config_id",
                table: "notification_rule",
                column: "channel_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_alias",
                table: "projects",
                column: "alias",
                unique: true,
                filter: "\"alias\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_service_networks_network_id",
                table: "service_networks",
                column: "network_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_service_id",
                table: "service_registry",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_volumes_service_id",
                table: "service_volumes",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_services_environment_id_alias",
                table: "services",
                columns: new[] { "environment_id", "alias" },
                unique: true,
                filter: "\"alias\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_services_GitCredentialsId",
                table: "services",
                column: "GitCredentialsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deployments");

            migrationBuilder.DropTable(
                name: "envs");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "haven_settings");

            migrationBuilder.DropTable(
                name: "notification_attempt");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "service_networks");

            migrationBuilder.DropTable(
                name: "service_registry");

            migrationBuilder.DropTable(
                name: "service_volumes");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "notification_rule");

            migrationBuilder.DropTable(
                name: "networks");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "notification_channel_config");

            migrationBuilder.DropTable(
                name: "environments");

            migrationBuilder.DropTable(
                name: "git_credentials");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
