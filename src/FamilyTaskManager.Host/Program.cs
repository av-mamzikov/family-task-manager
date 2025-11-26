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

// Add Serilog
builder.Services.AddSerilog();

// Register services by layer (Clean Architecture)
var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
var logger = loggerFactory.CreateLogger("Startup");

builder.Services
  .AddCoreServices() // Domain layer
  .AddUseCasesServices() // Application layer
  .AddInfrastructureServices(builder.Configuration, logger); // Infrastructure layer

// Register Mediator (must be in Host where SourceGenerator can scan all assemblies)
builder.Services.AddMediator(options =>
{
  options.ServiceLifetime = ServiceLifetime.Scoped;

  // Explicitly specify assemblies to scan for handlers
  // This ensures the generator only looks in the right places
  options.Assemblies =
  [
    typeof(CreateFamilyCommand).Assembly, // UseCases
    typeof(Family).Assembly // Core
  ];

  // Make generated types internal to avoid conflicts with test projects
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
Log.Information("Worker Module: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, PetMoodCalculator)");

app.Run();

// Make Program class accessible to WebApplicationFactory in tests
public partial class Program
{
}
