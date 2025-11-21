# FamilyTaskManager.Worker

Background worker service that handles scheduled tasks using Quartz.NET.

## Overview

This worker runs three main jobs:

1. **TaskInstanceCreatorJob** - Creates `TaskInstance` from `TaskTemplate` based on Quartz Cron schedules
2. **TaskReminderJob** - Sends reminders to family members for tasks due within the next hour
3. **PetMoodCalculatorJob** - Recalculates mood scores for all pets based on task completion

## Jobs

### TaskInstanceCreatorJob
- **Schedule**: Every minute (`0 * * * * ?`)
- **Purpose**: Checks all active `TaskTemplate` records and creates new `TaskInstance` when their cron schedule triggers
- **Invariant**: Only creates a new instance if the previous one is completed
- **Implementation**: Uses Quartz `CronExpression` to parse and evaluate schedules

### TaskReminderJob
- **Schedule**: Every 15 minutes (`0 */15 * * * ?`)
- **Purpose**: Sends Telegram notifications to family members for tasks due in the next hour
- **Note**: Currently logs reminders; full Telegram integration pending

### PetMoodCalculatorJob
- **Schedule**: Every 30 minutes (`0 */30 * * * ?`)
- **Purpose**: Recalculates `Pet.MoodScore` based on task completion status
- **Formula**: Implements the mood calculation from TЗ section 2.2
  - Completed on time: full points
  - Completed late: 50% of points
  - Overdue: negative contribution based on days overdue

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=postgres"
  },
  "Quartz": {
    "quartz.scheduler.instanceName": "FamilyTaskManagerScheduler",
    "quartz.jobStore.type": "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
    "quartz.jobStore.driverDelegateType": "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
    "quartz.dataSource.default.provider": "Npgsql"
  }
}
```

### User Secrets (Development)

For local development, store the connection string in user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=postgres"
```

## Dependencies

- **Quartz.NET 3.13.1** - Job scheduling
- **Quartz.Extensions.Hosting** - Integration with .NET hosting
- **Quartz.Serialization.Json** - JSON serialization for job data
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider
- **Mediator** - CQRS pattern implementation
- **Serilog** - Structured logging

## Running the Worker

### Development

```bash
cd src/FamilyTaskManager.Worker
dotnet run
```

### Production

```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
dotnet FamilyTaskManager.Worker.dll
```

## Database Setup

The worker automatically applies EF Core migrations on startup. Ensure PostgreSQL is running and accessible.

Quartz.NET requires its own tables in the database. These are created automatically on first run.

## Monitoring

The worker logs all job executions:
- Job start/completion
- Number of tasks created/reminders sent/pets updated
- Errors and warnings

Example log output:
```
[INF] TaskInstanceCreatorJob started at 2025-11-21T12:00:00Z
[INF] Found 5 active task templates
[INF] Creating TaskInstance for template abc123 (Feed cat), due at 2025-11-21T12:00:00Z
[INF] Successfully created TaskInstance xyz789 from template abc123
[INF] TaskInstanceCreatorJob completed. Created 3 new task instances
```

## Architecture

```
FamilyTaskManager.Worker/
├── Jobs/
│   ├── TaskInstanceCreatorJob.cs      # Creates TaskInstance from templates
│   ├── TaskReminderJob.cs             # Sends task reminders
│   └── PetMoodCalculatorJob.cs        # Recalculates pet mood scores
├── Program.cs                         # DI configuration and startup
├── appsettings.json                   # Configuration
└── README.md                          # This file
```

## Use Cases Integration

The worker uses the following Use Cases via Mediator:

- `GetActiveTaskTemplatesQuery` - Get all active task templates
- `CreateTaskInstanceFromTemplateCommand` - Create a new task instance
- `GetTasksDueForReminderQuery` - Get tasks requiring reminders
- `GetUserByIdQuery` - Get user details for notifications
- `GetAllPetsQuery` - Get all pets for mood calculation
- `CalculatePetMoodScoreCommand` - Calculate and update pet mood

## Future Enhancements

- [ ] Telegram notification service integration
- [ ] Configurable job schedules via database
- [ ] Job execution history and metrics
- [ ] Dead letter queue for failed jobs
- [ ] Health checks endpoint
- [ ] Distributed locking for horizontal scaling
