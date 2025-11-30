using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FamilyHardDelete : Migration
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_CompletedByMemberId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_FamilyMembers_StartedByMemberId",
                table: "TaskInstances");

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
    }
}
