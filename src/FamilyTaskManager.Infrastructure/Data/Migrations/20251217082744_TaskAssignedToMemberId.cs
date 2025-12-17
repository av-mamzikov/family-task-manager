using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaskAssignedToMemberId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToMemberId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_AssignedToMemberId",
                table: "TaskInstances",
                column: "AssignedToMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_FamilyMembers_AssignedToMemberId",
                table: "TaskInstances",
                column: "AssignedToMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_AssignedToMemberId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_AssignedToMemberId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "AssignedToMemberId",
                table: "TaskInstances");
        }
    }
}
