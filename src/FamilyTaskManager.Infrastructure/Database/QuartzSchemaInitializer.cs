using System.Data;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Interfaces;
using Npgsql;

namespace FamilyTaskManager.Infrastructure.Database;

public class QuartzSchemaInitializer : IQuartzSchemaInitializer
{
  public async Task InitializeAsync(AppDbContext dbContext, ILogger logger)
  {
    try
    {
      // Check if Quartz tables exist
      var quartzTablesExist = await CheckQuartzTablesExistAsync(dbContext);

      if (!quartzTablesExist)
      {
        logger.LogInformation("Quartz tables not found, creating schema...");
        await CreateQuartzSchemaAsync(dbContext);
        logger.LogInformation("Quartz schema created successfully");
      }
      else
      {
        logger.LogInformation("Quartz tables already exist");
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to initialize Quartz schema");
      throw;
    }
  }

  private static async Task<bool> CheckQuartzTablesExistAsync(AppDbContext dbContext)
  {
    var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection;
    if (connection == null)
    {
      throw new InvalidOperationException("Expected Npgsql connection");
    }

    var wasOpen = connection.State == ConnectionState.Open;
    if (!wasOpen)
    {
      await connection.OpenAsync();
    }

    try
    {
      using var command = connection.CreateCommand();
      command.CommandText = @"
        SELECT COUNT(*) 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name IN (
          'qrtz_job_details', 'qrtz_triggers', 'qrtz_simple_triggers', 
          'qrtz_cron_triggers', 'qrtz_simprop_triggers', 'qrtz_blob_triggers',
          'qrtz_calendars', 'qrtz_fired_triggers', 'qrtz_paused_trigger_grps',
          'qrtz_scheduler_state', 'qrtz_locks'
        )";

      var result = await command.ExecuteScalarAsync();
      var count = Convert.ToInt32(result);

      // All 11 required tables must exist
      return count == 11;
    }
    finally
    {
      if (!wasOpen)
      {
        await connection.CloseAsync();
      }
    }
  }

  private static async Task CreateQuartzSchemaAsync(AppDbContext dbContext)
  {
    var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection;
    if (connection == null)
    {
      throw new InvalidOperationException("Expected Npgsql connection");
    }

    var wasOpen = connection.State == ConnectionState.Open;
    if (!wasOpen)
    {
      await connection.OpenAsync();
    }

    try
    {
      // Split the large SQL script into individual statements for better reliability
      var sqlStatements = new[]
      {
        @"CREATE TABLE IF NOT EXISTS qrtz_job_details (
          SCHED_NAME VARCHAR(120) NOT NULL,
          JOB_NAME VARCHAR(200) NOT NULL,
          JOB_GROUP VARCHAR(200) NOT NULL,
          DESCRIPTION VARCHAR(250) NULL,
          JOB_CLASS_NAME VARCHAR(250) NOT NULL,
          IS_DURABLE BOOLEAN NOT NULL,
          IS_NONCONCURRENT BOOLEAN NOT NULL,
          IS_UPDATE_DATA BOOLEAN NOT NULL,
          REQUESTS_RECOVERY BOOLEAN NOT NULL,
          JOB_DATA BYTEA NULL,
          PRIMARY KEY (SCHED_NAME, JOB_NAME, JOB_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          JOB_NAME VARCHAR(200) NOT NULL,
          JOB_GROUP VARCHAR(200) NOT NULL,
          DESCRIPTION VARCHAR(250) NULL,
          NEXT_FIRE_TIME BIGINT NULL,
          PREV_FIRE_TIME BIGINT NULL,
          PRIORITY INTEGER NULL,
          TRIGGER_STATE VARCHAR(16) NOT NULL,
          TRIGGER_TYPE VARCHAR(8) NOT NULL,
          START_TIME BIGINT NOT NULL,
          END_TIME BIGINT NULL,
          CALENDAR_NAME VARCHAR(200) NULL,
          MISFIRE_INSTR SMALLINT NULL,
          JOB_DATA BYTEA NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
          FOREIGN KEY (SCHED_NAME, JOB_NAME, JOB_GROUP) REFERENCES QRTZ_JOB_DETAILS(SCHED_NAME, JOB_NAME, JOB_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_simple_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          REPEAT_COUNT BIGINT NOT NULL,
          REPEAT_INTERVAL BIGINT NOT NULL,
          TIMES_TRIGGERED BIGINT NOT NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
          FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP) REFERENCES QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_cron_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          CRON_EXPRESSION VARCHAR(120) NOT NULL,
          TIME_ZONE_ID VARCHAR(80) NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
          FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP) REFERENCES QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_simprop_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          STR_PROP_1 VARCHAR(512) NULL,
          STR_PROP_2 VARCHAR(512) NULL,
          STR_PROP_3 VARCHAR(512) NULL,
          INT_PROP_1 INTEGER NULL,
          INT_PROP_2 INTEGER NULL,
          LONG_PROP_1 BIGINT NULL,
          LONG_PROP_2 BIGINT NULL,
          DEC_PROP_1 NUMERIC(13,4) NULL,
          DEC_PROP_2 NUMERIC(13,4) NULL,
          BOOL_PROP_1 BOOLEAN NULL,
          BOOL_PROP_2 BOOLEAN NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
          FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP) REFERENCES QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_blob_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          BLOB_DATA BYTEA NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP),
          FOREIGN KEY (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP) REFERENCES QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_calendars (
          SCHED_NAME VARCHAR(120) NOT NULL,
          CALENDAR_NAME VARCHAR(200) NOT NULL,
          CALENDAR BYTEA NOT NULL,
          PRIMARY KEY (SCHED_NAME, CALENDAR_NAME)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_fired_triggers (
          SCHED_NAME VARCHAR(120) NOT NULL,
          ENTRY_ID VARCHAR(95) NOT NULL,
          TRIGGER_NAME VARCHAR(200) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          INSTANCE_NAME VARCHAR(200) NOT NULL,
          FIRED_TIME BIGINT NOT NULL,
          SCHED_TIME BIGINT NOT NULL,
          PRIORITY INTEGER NOT NULL,
          STATE VARCHAR(16) NOT NULL,
          JOB_NAME VARCHAR(200) NULL,
          JOB_GROUP VARCHAR(200) NULL,
          IS_NONCONCURRENT BOOLEAN NULL,
          REQUESTS_RECOVERY BOOLEAN NULL,
          PRIMARY KEY (SCHED_NAME, ENTRY_ID)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_paused_trigger_grps (
          SCHED_NAME VARCHAR(120) NOT NULL,
          TRIGGER_GROUP VARCHAR(200) NOT NULL,
          PRIMARY KEY (SCHED_NAME, TRIGGER_GROUP)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_scheduler_state (
          SCHED_NAME VARCHAR(120) NOT NULL,
          INSTANCE_NAME VARCHAR(200) NOT NULL,
          LAST_CHECKIN_TIME BIGINT NOT NULL,
          CHECKIN_INTERVAL BIGINT NOT NULL,
          PRIMARY KEY (SCHED_NAME, INSTANCE_NAME)
        )",
        @"CREATE TABLE IF NOT EXISTS qrtz_locks (
          SCHED_NAME VARCHAR(120) NOT NULL,
          LOCK_NAME VARCHAR(40) NOT NULL,
          PRIMARY KEY (SCHED_NAME, LOCK_NAME)
        )",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_j_req_recovery ON qrtz_job_details(SCHED_NAME, REQUESTS_RECOVERY)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_j_grp ON qrtz_job_details(SCHED_NAME, JOB_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_j ON qrtz_triggers(SCHED_NAME, JOB_NAME, JOB_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_jg ON qrtz_triggers(SCHED_NAME, JOB_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_c ON qrtz_triggers(SCHED_NAME, CALENDAR_NAME)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_g ON qrtz_triggers(SCHED_NAME, TRIGGER_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_state ON qrtz_triggers(SCHED_NAME, TRIGGER_STATE)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_n_state ON qrtz_triggers(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP, TRIGGER_STATE)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_n_g_state ON qrtz_triggers(SCHED_NAME, TRIGGER_GROUP, TRIGGER_STATE)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_next_fire_time ON qrtz_triggers(SCHED_NAME, NEXT_FIRE_TIME)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_nft_st ON qrtz_triggers(SCHED_NAME, TRIGGER_STATE, NEXT_FIRE_TIME)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_nft_misfire ON qrtz_triggers(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_nft_st_misfire ON qrtz_triggers(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME, TRIGGER_STATE)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_t_nft_st_misfire_grp ON qrtz_triggers(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME, TRIGGER_GROUP, TRIGGER_STATE)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_trig_inst_name ON qrtz_fired_triggers(SCHED_NAME, INSTANCE_NAME)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_inst_job_req_rcvry ON qrtz_fired_triggers(SCHED_NAME, INSTANCE_NAME, REQUESTS_RECOVERY)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_j_g ON qrtz_fired_triggers(SCHED_NAME, JOB_NAME, JOB_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_jg ON qrtz_fired_triggers(SCHED_NAME, JOB_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_t_g ON qrtz_fired_triggers(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP)",
        @"CREATE INDEX IF NOT EXISTS idx_qrtz_ft_tg ON qrtz_fired_triggers(SCHED_NAME, TRIGGER_GROUP)"
      };

      // Execute each statement individually for better error handling and reliability
      foreach (var sql in sqlStatements)
      {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30; // 30 seconds timeout per statement
        await command.ExecuteNonQueryAsync();
      }
    }
    finally
    {
      if (!wasOpen)
      {
        await connection.CloseAsync();
      }
    }
  }
}
