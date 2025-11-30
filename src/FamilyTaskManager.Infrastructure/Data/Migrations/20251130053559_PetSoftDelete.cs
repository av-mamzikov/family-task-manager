using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PetSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_TaskTemplates_TemplateId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_FamilyId_IsActive",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TaskTemplates");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ActionHistory_FamilyId_UserId_CreatedAt",
                table: "ActionHistory",
                columns: new[] { "FamilyId", "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_TaskTemplates_TemplateId",
                table: "TaskInstances",
                column: "TemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_TaskTemplates_TemplateId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_ActionHistory_FamilyId_UserId_CreatedAt",
                table: "ActionHistory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pets");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TaskTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_FamilyId_IsActive",
                table: "TaskTemplates",
                columns: new[] { "FamilyId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_TaskTemplates_TemplateId",
                table: "TaskInstances",
                column: "TemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id");
        }
    }
}
