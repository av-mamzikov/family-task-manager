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
            // In some environments (e.g. fresh test databases) Quartz tables may not exist yet.
            // Use IF EXISTS guards so the migration is safe to run without Quartz schema.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_cron_triggers'
  ) THEN
    DELETE FROM qrtz_cron_triggers WHERE trigger_name = 'PetMoodCalculatorJob-trigger';
  END IF;
END
$$;
            ");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_triggers'
  ) THEN
    DELETE FROM qrtz_triggers WHERE job_name = 'PetMoodCalculatorJob';
  END IF;
END
$$;
            ");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_job_details'
  ) THEN
    DELETE FROM qrtz_job_details WHERE job_name = 'PetMoodCalculatorJob';
  END IF;
END
$$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_cron_triggers'
  ) THEN
    DELETE FROM qrtz_cron_triggers WHERE trigger_name = 'SpotMoodCalculatorJob-trigger';
  END IF;
END
$$;
            ");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_triggers'
  ) THEN
    DELETE FROM qrtz_triggers WHERE job_name = 'SpotMoodCalculatorJob';
  END IF;
END
$$;
            ");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'qrtz_job_details'
  ) THEN
    DELETE FROM qrtz_job_details WHERE job_name = 'SpotMoodCalculatorJob';
  END IF;
END
$$;
            ");
        }
    }
}
