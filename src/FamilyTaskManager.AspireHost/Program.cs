using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container
var postgres = builder.AddPostgres("postgres")
  .WithLifetime(ContainerLifetime.Persistent);

// Add the database
var cleanArchDb = postgres.AddDatabase("FamilyTaskManager");

// Add the host project with the database connection
builder.AddProject<Projects.FamilyTaskManager_Host>("host")
  .WithReference(cleanArchDb)
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WaitFor(cleanArchDb);

builder
  .Build()
  .Run();
