using FamilyTaskManager.Infrastructure;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.UseCases;
using FamilyTaskManager.Worker.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateLogger();

try
{
  Log.Information("Starting FamilyTaskManager Worker");

  var builder = Host.CreateApplicationBuilder(args);

  // Add Serilog
  builder.Services.AddSerilog();

  // Add DbContext
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

  // Add Infrastructure services
  var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
  var logger = loggerFactory.CreateLogger("Infrastructure");
  builder.Services.AddInfrastructureServices(builder.Configuration, logger);

  // Add Mediator
  builder.Services.AddMediator(options =>
  {
    options.ServiceLifetime = ServiceLifetime.Scoped;
  });

  // Add Quartz services
  builder.Services.AddQuartz(q =>
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
  builder.Services.AddQuartzHostedService(options =>
  {
    options.WaitForJobsToComplete = true;
  });

  var host = builder.Build();

  // Ensure database is created and migrated
  using (var scope = host.Services.CreateScope())
  {
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    Log.Information("Database migration completed");
  }

  await host.RunAsync();
}
catch (Exception ex)
{
  Log.Fatal(ex, "Worker terminated unexpectedly");
  return 1;
}
finally
{
  await Log.CloseAndFlushAsync();
}

return 0;
