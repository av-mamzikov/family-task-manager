using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PetTaskDeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Pets_PetId",
                table: "TaskTemplates",
                column: "PetId",
                principalTable: "Pets",
                principalColumn: "Id");
        }
    }
}
