using FamilyTaskManager.Host.Modules.Bot;
using FamilyTaskManager.Host.Modules.Worker;
using FamilyTaskManager.Core;
using FamilyTaskManager.UseCases;
using FamilyTaskManager.Infrastructure;
using FamilyTaskManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateLogger();

try
{
  Log.Information("Starting FamilyTaskManager Host (Modular Monolith)");

  var builder = Host.CreateApplicationBuilder(args);

  // Add Serilog
  builder.Services.AddSerilog();

  // Register services by layer (Clean Architecture)
  var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
  var logger = loggerFactory.CreateLogger("Startup");
  
  builder.Services
    .AddCoreServices()                                    // Domain layer
    .AddUseCasesServices()                                // Application layer
    .AddInfrastructureServices(builder.Configuration, logger);  // Infrastructure layer
  
  // Register Mediator (must be in Host where SourceGenerator can scan all assemblies)
  builder.Services.AddMediator(options =>
  {
    options.ServiceLifetime = ServiceLifetime.Scoped;
  });

  // Register modules
  builder.Services
    .AddBotModule(builder.Configuration, logger)
    .AddWorkerModule(builder.Configuration, logger);

  var host = builder.Build();

  // Ensure database is created and migrated
  using (var scope = host.Services.CreateScope())
  {
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    Log.Information("Database migration completed");
  }

  Log.Information("All modules registered successfully");
  Log.Information("Bot Module: Telegram Bot with Long Polling");
  Log.Information("Worker Module: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, PetMoodCalculator)");

  await host.RunAsync();
}
catch (Exception ex)
{
  Log.Fatal(ex, "Host terminated unexpectedly");
  return 1;
}
finally
{
  await Log.CloseAndFlushAsync();
}

return 0;
