using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_CompletedBy",
                table: "TaskInstances",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CreatedBy",
                table: "Invitations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActionHistory_UserId",
                table: "ActionHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionHistory_Families_FamilyId",
                table: "ActionHistory",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionHistory_Users_UserId",
                table: "ActionHistory",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Families_FamilyId",
                table: "Invitations",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_CreatedBy",
                table: "Invitations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pets_Families_FamilyId",
                table: "Pets",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Families_FamilyId",
                table: "TaskInstances",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Users_CompletedBy",
                table: "TaskInstances",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Families_FamilyId",
                table: "TaskTemplates",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionHistory_Families_FamilyId",
                table: "ActionHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionHistory_Users_UserId",
                table: "ActionHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Families_FamilyId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_CreatedBy",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Pets_Families_FamilyId",
                table: "Pets");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Families_FamilyId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_TaskTemplates_TemplateId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Users_CompletedBy",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Families_FamilyId",
                table: "TaskTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_CompletedBy",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_CreatedBy",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_ActionHistory_UserId",
                table: "ActionHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
