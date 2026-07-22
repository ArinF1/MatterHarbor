using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatterHarbor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "matterharbor");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "matterharbor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "matterharbor",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => new { x.OrganizationId, x.Key });
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "matterharbor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "matterharbor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_users",
                schema: "matterharbor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_users_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "matterharbor",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cases",
                schema: "matterharbor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cases_organization_users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalSchema: "matterharbor",
                        principalTable: "organization_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cases_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "matterharbor",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_OrganizationId_EntityId_OccurredAt",
                schema: "matterharbor",
                table: "audit_entries",
                columns: new[] { "OrganizationId", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_cases_AssignedUserId",
                schema: "matterharbor",
                table: "cases",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cases_OrganizationId_CaseNumber",
                schema: "matterharbor",
                table: "cases",
                columns: new[] { "OrganizationId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cases_OrganizationId_CreatedAt",
                schema: "matterharbor",
                table: "cases",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_OrganizationId_ExternalSubject",
                schema: "matterharbor",
                table: "organization_users",
                columns: new[] { "OrganizationId", "ExternalSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OrganizationId_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages",
                columns: new[] { "OrganizationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_LockedUntil_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages",
                columns: new[] { "Status", "LockedUntil", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "matterharbor");

            migrationBuilder.DropTable(
                name: "cases",
                schema: "matterharbor");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "matterharbor");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "matterharbor");

            migrationBuilder.DropTable(
                name: "organization_users",
                schema: "matterharbor");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "matterharbor");
        }
    }
}
