using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventsRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEventOutbox_Status_DeliveryMode_OccurredAtUtc",
                table: "DomainEventOutbox");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "DomainEventOutbox");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_Status_OccurredAtUtc",
                table: "DomainEventOutbox",
                columns: new[] { "Status", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEventOutbox_Status_OccurredAtUtc",
                table: "DomainEventOutbox");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMode",
                table: "DomainEventOutbox",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_Status_DeliveryMode_OccurredAtUtc",
                table: "DomainEventOutbox",
                columns: new[] { "Status", "DeliveryMode", "OccurredAtUtc" });
        }
    }
}
