using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FamilyMemberToUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyMembers_Users_UserId",
                table: "FamilyMembers");
        }
    }
}
