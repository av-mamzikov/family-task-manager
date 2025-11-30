using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaskStartedByMemberId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Users_CompletedBy",
                table: "TaskInstances");

            migrationBuilder.RenameColumn(
                name: "CompletedBy",
                table: "TaskInstances",
                newName: "StartedByMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskInstances_CompletedBy",
                table: "TaskInstances",
                newName: "IX_TaskInstances_StartedByMemberId");

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByMemberId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_CompletedByMemberId",
                table: "TaskInstances",
                column: "CompletedByMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_FamilyMembers_CompletedByMemberId",
                table: "TaskInstances",
                column: "CompletedByMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_FamilyMembers_StartedByMemberId",
                table: "TaskInstances",
                column: "StartedByMemberId",
                principalTable: "FamilyMembers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_StartedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.RenameColumn(
                name: "StartedByMemberId",
                table: "TaskInstances",
                newName: "CompletedBy");

            migrationBuilder.RenameIndex(
                name: "IX_TaskInstances_StartedByMemberId",
                table: "TaskInstances",
                newName: "IX_TaskInstances_CompletedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Users_CompletedBy",
                table: "TaskInstances",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
