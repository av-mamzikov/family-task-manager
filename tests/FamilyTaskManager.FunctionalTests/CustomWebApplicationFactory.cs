using FamilyTaskManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot;
using Testcontainers.PostgreSql;

namespace FamilyTaskManager.FunctionalTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
  where TProgram : class
{
  private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("familytask_test")
    .WithUsername("test")
    .WithPassword("test123")
    .Build();

  /// <summary>
  ///   Test Telegram bot client for verifying bot interactions in tests
  /// </summary>
  public TestTelegramBotClient TelegramBotClient { get; } = new();

  public async Task InitializeAsync() => await _dbContainer.StartAsync();

  public new async Task DisposeAsync() => await _dbContainer.DisposeAsync();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureServices(services =>
    {
      // Remove the app's DbContext registration
      services.RemoveAll<AppDbContext>();
      services.RemoveAll<DbContextOptions<AppDbContext>>();

      // Add DbContext using the Testcontainers PostgreSQL instance
      services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(_dbContainer.GetConnectionString()); });

      // Remove the real ITelegramBotClient registration
      services.RemoveAll<ITelegramBotClient>();

      // Use test implementation to avoid real Telegram API calls
      services.AddSingleton<ITelegramBotClient>(TelegramBotClient);

      // Remove QuartzHostedService to prevent scheduler initialization in tests
      var quartzHostedService = services
        .FirstOrDefault(d => d.ImplementationType?.Name == "QuartzHostedService");
      if (quartzHostedService != null)
      {
        services.Remove(quartzHostedService);
      }
    });

    // Configure services to run database migrations after host is built
    builder.ConfigureServices(services => { services.AddHostedService<DatabaseInitializer>(); });
  }

  private class DatabaseInitializer : IHostedService
  {
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
      _serviceProvider = serviceProvider;
      _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      using var scope = _serviceProvider.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      try
      {
        // Apply migrations to create the database schema
        await db.Database.MigrateAsync(cancellationToken);

        // Seed the database with test data
        await SeedData.PopulateTestDataAsync(db);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
      }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  }
}
