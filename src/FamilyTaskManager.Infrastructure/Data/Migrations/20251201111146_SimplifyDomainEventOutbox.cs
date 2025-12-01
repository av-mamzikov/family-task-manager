using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDomainEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEventOutbox_AggregateId",
                table: "DomainEventOutbox");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                table: "DomainEventOutbox");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                table: "DomainEventOutbox");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AggregateId",
                table: "DomainEventOutbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                table: "DomainEventOutbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventOutbox_AggregateId",
                table: "DomainEventOutbox",
                column: "AggregateId");
        }
    }
}
