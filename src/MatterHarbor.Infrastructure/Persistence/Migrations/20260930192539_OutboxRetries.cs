using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatterHarbor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_Status_LockedUntil_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                schema: "matterharbor",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                schema: "matterharbor",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_NextAttemptAt_LockedUntil_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages",
                columns: new[] { "Status", "NextAttemptAt", "LockedUntil", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_Status_NextAttemptAt_LockedUntil_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                schema: "matterharbor",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "matterharbor",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_LockedUntil_OccurredAt",
                schema: "matterharbor",
                table: "outbox_messages",
                columns: new[] { "Status", "LockedUntil", "OccurredAt" });
        }
    }
}
