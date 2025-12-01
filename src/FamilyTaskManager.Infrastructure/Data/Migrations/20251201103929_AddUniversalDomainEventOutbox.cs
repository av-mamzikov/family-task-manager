using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversalDomainEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainEventOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    DeliveryMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEventOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_AggregateId",
                table: "DomainEventOutbox",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_EventType_Status",
                table: "DomainEventOutbox",
                columns: new[] { "EventType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_ProcessedAtUtc",
                table: "DomainEventOutbox",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_Status_DeliveryMode_OccurredAtUtc",
                table: "DomainEventOutbox",
                columns: new[] { "Status", "DeliveryMode", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainEventOutbox");
        }
    }
}
