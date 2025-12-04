using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTaskManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameQuartzPetJobToSpotJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM qrtz_cron_triggers 
                WHERE trigger_name = 'PetMoodCalculatorJob-trigger';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM qrtz_triggers 
                WHERE job_name = 'PetMoodCalculatorJob';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM qrtz_job_details 
                WHERE job_name = 'PetMoodCalculatorJob';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM qrtz_cron_triggers 
                WHERE trigger_name = 'SpotMoodCalculatorJob-trigger';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM qrtz_triggers 
                WHERE job_name = 'SpotMoodCalculatorJob';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM qrtz_job_details 
                WHERE job_name = 'SpotMoodCalculatorJob';
            ");
        }
    }
}
