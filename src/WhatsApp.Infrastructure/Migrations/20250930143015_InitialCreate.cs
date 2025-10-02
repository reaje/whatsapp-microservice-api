using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "whatsapp_service");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "whatsapp_service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    settings = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_agents",
                schema: "whatsapp_service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    configuration = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_agents", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_agents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_sessions",
                schema: "whatsapp_service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    session_data = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversations",
                schema: "whatsapp_service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_conversations_ai_agents_agent_id",
                        column: x => x.agent_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "ai_agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_conversations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_conversations_whatsapp_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "whatsapp_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "whatsapp_service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    from_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ai_processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_whatsapp_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "whatsapp_service",
                        principalTable: "whatsapp_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_agents_tenant_id",
                schema: "whatsapp_service",
                table: "ai_agents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversations_agent_id",
                schema: "whatsapp_service",
                table: "ai_conversations",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversations_session_id",
                schema: "whatsapp_service",
                table: "ai_conversations",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversations_tenant_id",
                schema: "whatsapp_service",
                table: "ai_conversations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_created_at",
                schema: "whatsapp_service",
                table: "messages",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_messages_message_id",
                schema: "whatsapp_service",
                table: "messages",
                column: "message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_session_id",
                schema: "whatsapp_service",
                table: "messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_tenant_id",
                schema: "whatsapp_service",
                table: "messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_client_id",
                schema: "whatsapp_service",
                table: "tenants",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_sessions_tenant_id",
                schema: "whatsapp_service",
                table: "whatsapp_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_sessions_tenant_id_phone_number",
                schema: "whatsapp_service",
                table: "whatsapp_sessions",
                columns: new[] { "tenant_id", "phone_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_conversations",
                schema: "whatsapp_service");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "whatsapp_service");

            migrationBuilder.DropTable(
                name: "ai_agents",
                schema: "whatsapp_service");

            migrationBuilder.DropTable(
                name: "whatsapp_sessions",
                schema: "whatsapp_service");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "whatsapp_service");
        }
    }
}
