using FamilyTaskManager.Bot;
using FamilyTaskManager.Bot.Configuration;
using FamilyTaskManager.Bot.Services;
using FamilyTaskManager.Bot.Handlers;
using FamilyTaskManager.Infrastructure;
using FamilyTaskManager.Infrastructure.Data;
using Serilog;
using Telegram.Bot.Polling;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .CreateLogger();

try
{
  var builder = Host.CreateApplicationBuilder(args);

  // Add Serilog
  builder.Services.AddSerilog();

  // Configure Bot
  builder.Services.Configure<BotConfiguration>(
    builder.Configuration.GetSection("Bot"));

  // Add Infrastructure (EF Core, Repositories)
  using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog());
  var logger = loggerFactory.CreateLogger<Program>();
  builder.Services.AddInfrastructureServices(builder.Configuration, logger);

  // Add Bot Services
  builder.Services.AddSingleton<ISessionManager, SessionManager>();
  builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
  builder.Services.AddScoped<ICommandHandler, CommandHandler>();
  builder.Services.AddScoped<ICallbackQueryHandler, CallbackQueryHandler>();
  builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
  
  // Add Command Handlers
  builder.Services.AddScoped<FamilyTaskManager.Bot.Handlers.Commands.FamilyCommandHandler>();
  builder.Services.AddScoped<FamilyTaskManager.Bot.Handlers.Commands.TasksCommandHandler>();
  builder.Services.AddScoped<FamilyTaskManager.Bot.Handlers.Commands.PetCommandHandler>();
  builder.Services.AddScoped<FamilyTaskManager.Bot.Handlers.Commands.StatsCommandHandler>();

  // Add Worker
  builder.Services.AddHostedService<Worker>();

  var host = builder.Build();
  
  Log.Information("Starting Telegram Bot...");
  host.Run();
}
catch (Exception ex)
{
  Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
  Log.CloseAndFlush();
}
