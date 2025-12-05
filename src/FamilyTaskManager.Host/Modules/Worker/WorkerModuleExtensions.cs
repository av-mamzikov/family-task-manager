using FamilyTaskManager.Host.Modules.Worker.Jobs;
using FamilyTaskManager.Infrastructure;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker;

public static class WorkerModuleExtensions
{
  public static IServiceCollection AddWorkerModule(
    this IServiceCollection services,
    IConfiguration configuration,
    ILogger? logger = null)
  {
    logger?.LogInformation("Registering Worker Module...");

    // Get database connection string using shared logic from Infrastructure
    var connectionString = configuration.GetDatabaseConnectionString();

    // Add Quartz services
    services.AddQuartz(q =>
    {
      // Use PostgreSQL for job persistence
      q.UsePersistentStore(store =>
      {
        store.UsePostgres(connectionString);
        store.UseNewtonsoftJsonSerializer();
      });

      // Job 1: TaskInstanceCreatorJob - runs every minute
      var taskCreatorJobKey = new JobKey("TaskInstanceCreatorJob");
      q.AddJob<TaskInstanceCreatorJob>(opts => opts.WithIdentity(taskCreatorJobKey));
      q.AddTrigger(opts => opts
        .ForJob(taskCreatorJobKey)
        .WithIdentity("TaskInstanceCreatorJob-trigger")
        .WithCronSchedule("10 * * * * ?")
        .WithDescription("Creates TaskInstance from TaskTemplate based on schedule"));

      // Job 2: TaskReminderJob - runs every 15 minutes
      var reminderJobKey = new JobKey("TaskReminderJob");
      q.AddJob<TaskReminderJob>(opts => opts.WithIdentity(reminderJobKey));
      q.AddTrigger(opts => opts
        .ForJob(reminderJobKey)
        .WithIdentity("TaskReminderJob-trigger")
        .WithCronSchedule("10 */15 * * * ?")
        .WithDescription("Sends reminders for tasks due within the next hour"));

      // Job 3: SpotMoodCalculatorJob - runs every 30 minutes
      var moodJobKey = new JobKey("SpotMoodCalculatorJob");
      q.AddJob<SpotMoodCalculatorJob>(opts => opts.WithIdentity(moodJobKey));
      q.AddTrigger(opts => opts
        .ForJob(moodJobKey)
        .WithIdentity("SpotMoodCalculatorJob-trigger")
        .WithCronSchedule("10 */30 * * * ?")
        .WithDescription("Recalculates mood scores for all spots"));

      // Register infrastructure jobs (OutboxDispatcherJob)
      q.AddInfrastructureJobs(logger);
    });

    // Add Quartz hosted service
    services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

    logger?.LogInformation(
      "Worker Module registered: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, SpotMoodCalculator, NotificationBatch)");

    return services;
  }
}
