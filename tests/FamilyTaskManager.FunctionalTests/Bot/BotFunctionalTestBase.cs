using FamilyTaskManager.Core;
using FamilyTaskManager.Host.Modules.Bot;
using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Infrastructure;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.UseCases;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.Bot;

/// <summary>
/// Base class for bot functional tests.
/// Provides infrastructure for testing bot handlers with an in-memory database and mocked Telegram API.
/// </summary>
public abstract class BotFunctionalTestBase : IAsyncLifetime
{
  protected readonly FakeTelegramBotClient FakeBotClient;
  protected ServiceProvider? ServiceProvider;
  protected AppDbContext? DbContext;
  protected IUpdateHandler? UpdateHandler;

  protected BotFunctionalTestBase()
  {
    FakeBotClient = new FakeTelegramBotClient();
  }

  public virtual async Task InitializeAsync()
  {
    // Build service provider with all necessary dependencies
    var services = new ServiceCollection();
    
    // Add logging
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    
    // Add configuration
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Bot:BotToken"] = "test_token_123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ"
      })
      .Build();
    services.AddSingleton<IConfiguration>(configuration);
    
    // Add DbContext with in-memory database
    services.AddDbContext<AppDbContext>(options =>
      options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
    
    // Add core services
    services.AddCoreServices();
    services.AddUseCasesServices();
    
    // Add infrastructure services (without real database)
    var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("TestStartup");
    services.AddInfrastructureServices(configuration, logger);
    
    // Override ITelegramBotClient with fake
    services.AddSingleton<ITelegramBotClient>(FakeBotClient.BotClient);
    
    // Add Mediator
    services.AddMediator(options =>
    {
      options.ServiceLifetime = ServiceLifetime.Scoped;
    });
    
    // Add bot handlers
    services.AddScoped<IUpdateHandler, UpdateHandler>();
    services.AddScoped<ICommandHandler, CommandHandler>();
    services.AddScoped<ICallbackQueryHandler, CallbackQueryHandler>();
    services.AddScoped<SessionManager>();
    services.AddSingleton<ISessionManager>(sp => sp.GetRequiredService<SessionManager>());
    
    ServiceProvider = services.BuildServiceProvider();
    
    // Get DbContext and ensure database is created
    using var scope = ServiceProvider.CreateScope();
    DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbContext.Database.EnsureCreatedAsync();
    
    // Get UpdateHandler
    UpdateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
  }

  public virtual async Task DisposeAsync()
  {
    if (DbContext != null)
    {
      await DbContext.Database.EnsureDeletedAsync();
      await DbContext.DisposeAsync();
    }
    
    if (ServiceProvider != null)
    {
      await ServiceProvider.DisposeAsync();
    }
    
    FakeBotClient.Clear();
  }

  /// <summary>
  /// Simulates sending an update to the bot and processing it.
  /// </summary>
  protected async Task SendUpdateAsync(Update update, CancellationToken cancellationToken = default)
  {
    if (ServiceProvider == null)
      throw new InvalidOperationException("ServiceProvider is not initialized. Call InitializeAsync first.");
    
    using var scope = ServiceProvider.CreateScope();
    var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
    
    await updateHandler.HandleUpdateAsync(FakeBotClient.BotClient, update, cancellationToken);
  }

  /// <summary>
  /// Simulates a user sending a text message to the bot.
  /// </summary>
  protected async Task SendTextMessageAsync(
    long chatId,
    long userId,
    string text,
    CancellationToken cancellationToken = default)
  {
    var update = TestUpdateFactory.CreateTextMessage(chatId, userId, text);
    await SendUpdateAsync(update, cancellationToken);
  }

  /// <summary>
  /// Simulates a user pressing a callback button.
  /// </summary>
  protected async Task SendCallbackQueryAsync(
    long chatId,
    long userId,
    string callbackData,
    int messageId = 1,
    CancellationToken cancellationToken = default)
  {
    var update = TestUpdateFactory.CreateCallbackQuery(chatId, userId, callbackData, messageId);
    await SendUpdateAsync(update, cancellationToken);
  }

  /// <summary>
  /// Simulates a user sending a command to the bot.
  /// </summary>
  protected async Task SendCommandAsync(
    long chatId,
    long userId,
    string command,
    CancellationToken cancellationToken = default)
  {
    var update = TestUpdateFactory.CreateCommand(chatId, userId, command);
    await SendUpdateAsync(update, cancellationToken);
  }
}
