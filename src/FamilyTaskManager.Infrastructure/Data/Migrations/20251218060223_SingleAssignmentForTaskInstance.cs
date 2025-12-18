using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SingleAssignmentForTaskInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_StartedByMemberId",
                table: "TaskInstances");

            migrationBuilder.Sql(
                "UPDATE \"TaskInstances\" " +
                "SET \"AssignedToMemberId\" = COALESCE(\"CompletedByMemberId\", \"StartedByMemberId\") " +
                "WHERE \"AssignedToMemberId\" IS NULL AND (\"CompletedByMemberId\" IS NOT NULL OR \"StartedByMemberId\" IS NOT NULL);");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_StartedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "StartedByMemberId",
                table: "TaskInstances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByMemberId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedByMemberId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"TaskInstances\" " +
                "SET \"StartedByMemberId\" = \"AssignedToMemberId\" " +
                "WHERE \"AssignedToMemberId\" IS NOT NULL AND \"Status\" = 'InProgress' AND \"StartedByMemberId\" IS NULL;");

            migrationBuilder.Sql(
                "UPDATE \"TaskInstances\" " +
                "SET \"CompletedByMemberId\" = \"AssignedToMemberId\" " +
                "WHERE \"AssignedToMemberId\" IS NOT NULL AND \"Status\" = 'Completed' AND \"CompletedByMemberId\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_CompletedByMemberId",
                table: "TaskInstances",
                column: "CompletedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_StartedByMemberId",
                table: "TaskInstances",
                column: "StartedByMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_FamilyMembers_CompletedByMemberId",
                table: "TaskInstances",
                column: "CompletedByMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_FamilyMembers_StartedByMemberId",
                table: "TaskInstances",
                column: "StartedByMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
