using FamilyTaskManager.Host.Modules.Worker.Jobs;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker;

public static class WorkerModuleExtensions
{
  public static IServiceCollection AddWorkerModule(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    // Add Quartz services
    services.AddQuartz(q =>
    {
      // Use PostgreSQL for job persistence
      q.UsePersistentStore(store =>
      {
        store.UsePostgres(connectionString!);
        store.UseNewtonsoftJsonSerializer();
      });

      // Job 1: TaskInstanceCreatorJob - runs every minute
      var taskCreatorJobKey = new JobKey("TaskInstanceCreatorJob");
      q.AddJob<TaskInstanceCreatorJob>(opts => opts.WithIdentity(taskCreatorJobKey));
      q.AddTrigger(opts => opts
        .ForJob(taskCreatorJobKey)
        .WithIdentity("TaskInstanceCreatorJob-trigger")
        .WithCronSchedule("0 * * * * ?") // Every minute at second 0
        .WithDescription("Creates TaskInstance from TaskTemplate based on schedule"));

      // Job 2: TaskReminderJob - runs every 15 minutes
      var reminderJobKey = new JobKey("TaskReminderJob");
      q.AddJob<TaskReminderJob>(opts => opts.WithIdentity(reminderJobKey));
      q.AddTrigger(opts => opts
        .ForJob(reminderJobKey)
        .WithIdentity("TaskReminderJob-trigger")
        .WithCronSchedule("0 */15 * * * ?") // Every 15 minutes
        .WithDescription("Sends reminders for tasks due within the next hour"));

      // Job 3: PetMoodCalculatorJob - runs every 30 minutes
      var moodJobKey = new JobKey("PetMoodCalculatorJob");
      q.AddJob<PetMoodCalculatorJob>(opts => opts.WithIdentity(moodJobKey));
      q.AddTrigger(opts => opts
        .ForJob(moodJobKey)
        .WithIdentity("PetMoodCalculatorJob-trigger")
        .WithCronSchedule("0 */30 * * * ?") // Every 30 minutes
        .WithDescription("Recalculates mood scores for all pets"));
    });

    // Add Quartz hosted service
    services.AddQuartzHostedService(options =>
    {
      options.WaitForJobsToComplete = true;
    });

    return services;
  }
}
