using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PetRenamedToSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates");

            migrationBuilder.RenameTable(
                name: "Pets",
                newName: "Spots");

            migrationBuilder.RenameColumn(
                name: "PetId",
                table: "TaskTemplates",
                newName: "SpotId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplates_PetId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplates_SpotId");

            migrationBuilder.RenameColumn(
                name: "PetId",
                table: "TaskInstances",
                newName: "SpotId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskInstances_PetId",
                table: "TaskInstances",
                newName: "IX_TaskInstances_SpotId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Spots_SpotId",
                table: "TaskInstances",
                column: "SpotId",
                principalTable: "Spots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Spots_SpotId",
                table: "TaskTemplates",
                column: "SpotId",
                principalTable: "Spots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_Spots_SpotId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Spots_SpotId",
                table: "TaskTemplates");

            migrationBuilder.RenameTable(
                name: "Spots",
                newName: "Pets");

            migrationBuilder.RenameColumn(
                name: "SpotId",
                table: "TaskTemplates",
                newName: "PetId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplates_SpotId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplates_PetId");

            migrationBuilder.RenameColumn(
                name: "SpotId",
                table: "TaskInstances",
                newName: "PetId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskInstances_SpotId",
                table: "TaskInstances",
                newName: "IX_TaskInstances_PetId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_Pets_PetId",
                table: "TaskInstances",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
