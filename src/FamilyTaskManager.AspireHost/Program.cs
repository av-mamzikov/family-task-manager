using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container with exposed port
var postgresPassword = builder.AddParameter("postgres-password");
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
  .WithLifetime(ContainerLifetime.Persistent)
  .WithPgAdmin();

// Add the database
var cleanArchDb = postgres.AddDatabase("FamilyTaskManager");

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
