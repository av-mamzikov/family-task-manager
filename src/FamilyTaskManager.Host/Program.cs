using FamilyTaskManager.Core;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host;
using FamilyTaskManager.Host.Modules.Bot;
using FamilyTaskManager.Host.Modules.Worker;
using FamilyTaskManager.Infrastructure;
using FamilyTaskManager.UseCases;
using FamilyTaskManager.UseCases.Families;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateLogger();

Log.Information("Starting FamilyTaskManager Host (Modular Monolith)");

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults for monitoring, health checks, and service discovery
builder.AddServiceDefaults();

builder.Services.AddSerilog();

var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
var logger = loggerFactory.CreateLogger("Startup");

// Register services by layer (Clean Architecture)
builder.Services
  .AddCoreServices()
  .AddUseCasesServices()
  .AddInfrastructureServices(builder.Configuration, logger);

builder.Services.AddMediator(options =>
{
  options.ServiceLifetime = ServiceLifetime.Scoped;
  options.Assemblies =
  [
    typeof(CreateFamilyCommand).Assembly, // UseCases
    typeof(Family).Assembly, // Core
    typeof(InfrastructureServiceExtensions).Assembly // Infrastructure (for event handlers)
  ];
  options.Namespace = "FamilyTaskManager.Host.Generated";
  options.GenerateTypesAsInternal = true;
});

// Register modules
builder.Services
  .AddBotModule(builder.Configuration, logger)
  .AddWorkerModule(builder.Configuration, logger);

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
  app.MapGet("/", () => "FamilyTaskManager is running");
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
  status = "Healthy",
  timestamp = DateTime.UtcNow,
  modules = new[] { "Bot", "Worker" }
}));

// Initialize infrastructure (database migrations, schema, etc.) only if not in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
  await app.InitializeInfrastructureAsync();
}

Log.Information("All modules registered successfully");
Log.Information("Bot Module: Telegram Bot with Long Polling");
Log.Information("Worker Module: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, SpotMoodCalculator)");

app.Run();

// Make Program class accessible to WebApplicationFactory in tests
public partial class Program
{
}
