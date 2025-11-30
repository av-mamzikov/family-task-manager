using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceScheduleStringWithValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Schedule",
                table: "TaskTemplates");

            migrationBuilder.AddColumn<int>(
                name: "ScheduleDayOfMonth",
                table: "TaskTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleDayOfWeek",
                table: "TaskTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduleTime",
                table: "TaskTemplates",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType",
                table: "TaskTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleDayOfMonth",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ScheduleDayOfWeek",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ScheduleTime",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "TaskTemplates");

            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                table: "TaskTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
