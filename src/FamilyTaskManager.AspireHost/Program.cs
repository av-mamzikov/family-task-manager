using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container
var postgres = builder.AddPostgres("postgres")
  .WithLifetime(ContainerLifetime.Persistent);

// Add the database
var cleanArchDb = postgres.AddDatabase("FamilyTaskManager");

// Add pgAdmin for database management
var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4")
  .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@familytask.com")
  .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin123")
  .WithEnvironment("PGADMIN_SERVER_JSON_FILE", "/pgadmin4/servers.json")
  .WithHttpEndpoint(targetPort: 80, port: 5050)
  .WithReference(postgres)
  .WithBindMount("servers.json", "/pgadmin4/servers.json")
  .WaitFor(postgres);

// Add the host project with the database connection
builder.AddProject<FamilyTaskManager_Host>("host")
  .WithReference(cleanArchDb, "DefaultConnection")
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WithEnvironment("Bot__BotToken", builder.Configuration["Bot:BotToken"] ?? "")
  .WithEnvironment("Bot__BotUsername", builder.Configuration["Bot:BotUsername"] ?? "")
  .WaitFor(cleanArchDb);

builder
  .Build()
  .Run();
