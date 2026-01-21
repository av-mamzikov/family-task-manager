using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CampaignId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "Users");
        }
    }
}
