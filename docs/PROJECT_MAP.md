# Family Task Manager - Project Map

## Overview

Family Task Manager is a Telegram bot for managing family chores with gamification elements, built on Clean Architecture
with a modular monolith design.

## Architecture Layers

### 1. Core Layer (`FamilyTaskManager.Core`)

**Domain Model & Business Logic**

- **Aggregates**:
    - `FamilyAggregate/`: Family, FamilyMember, Invitation, FamilyRole
    - `TaskAggregate/`: TaskInstance, TaskTemplate, Schedule, TaskPoints, TaskTitle
    - `SpotAggregate/`: Spot, SpotDisplay (pets, home zones, appliances)
    - `UserAggregate/`: User entity
- **ValueObjects/**: Domain value objects
- **Specifications/`: Specification pattern implementations
- **Interfaces/`: Core interfaces including:
    - `IAppRepository<T>`: Custom repository with GetOrCreateAndSaveAsync
    - `IAppReadRepository<T>`: Read-only repository
    - `IRepository<T>`, `IReadRepository<T>`: Base repository interfaces
- **Services/`: Domain services (TaskInstanceFactory, SpotMoodCalculator)
- **Points/`: Points system logic
- **Utils/`: Core utilities

### 2. UseCases Layer (`FamilyTaskManager.UseCases`)

**Application Layer - Command/Query Handlers**

- **Families/`: Family management use cases
- **Tasks/`: Task management use cases
- **Spots/`: Spot management use cases
- **Users/`: User management use cases
- **Statistics/`: Statistics and reporting use cases
- **Contracts/`: Request/Response DTOs
- **TaskTemplates/`: Predefined task templates
- **Constants.cs**: Application constants

### 3. Infrastructure Layer (`FamilyTaskManager.Infrastructure`)

**Data Access & External Services**

- **Data/`: EF Core DbContext and configurations
- **Database/`: Database-specific logic
- **Adapters/`: External service adapters
- **Services/`: Infrastructure services (TimeZoneService, TelegramNotificationService)
- **Jobs/`: Quartz.NET jobs
- **Notifications/`: Notification system
- **Email/`: Email services
- **Behaviors/`: MediatR pipeline behaviors
- **Interfaces/`: Infrastructure-specific interfaces
- **Migrations/`: Database migrations

### 4. Host Layer (`FamilyTaskManager.Host`)

**Modular Monolith Entry Point**

- **Modules/`:
    - **Bot/`: Telegram bot module
        - `Handlers/`: Telegram update handlers
        - `Services/`: Bot-specific services
        - `Models/`: Bot-specific models
        - `Helpers/`: Bot utilities
        - `Constants/`: Bot constants
        - `Configuration/`: Bot configuration
    - **Worker/`: Background job module
        - `Jobs/`: Host-specific Quartz jobs
- **Program.cs**: Main entry point with service registration
- **HostInitializationExtensions.cs**: Host initialization logic

### 5. AspireHost Layer (`FamilyTaskManager.AspireHost`)

**.NET Aspire Orchestration**

- **Program.cs**: Aspire orchestration setup
- Used for local development with service discovery and monitoring

### 6. ServiceDefaults (`FamilyTaskManager.ServiceDefaults`)

**Shared Service Configuration**

- Standardized service defaults for monitoring, health checks, etc.

## Test Suite

### 7. Test Projects (`tests/`)

- **FamilyTaskManager.UnitTests/`: Unit tests
- **FamilyTaskManager.FunctionalTests/`: Functional tests
- **FamilyTaskManager.IntegrationTests/`: Integration tests
- **FamilyTaskManager.E2ETests/`: End-to-end tests
- **FamilyTaskManager.AspireTests/`: Aspire-specific tests
- **FamilyTaskManager.TestInfrastructure/**: Shared test infrastructure

## Key Architectural Patterns

### Clean Architecture

- **Core**: Domain entities, value objects, business rules
- **UseCases**: Application logic, use cases
- **Infrastructure**: External concerns (data, notifications)
- **Host**: Composition root, presentation layer

### Repository Pattern

- Custom `IAppRepository<T>` with `GetOrCreateAndSaveAsync` method
- `IAppReadRepository<T>` for read-only operations
- EF Core implementations in Infrastructure

### CQRS with MediatR

- Command/Query separation
- MediatR for in-process messaging
- Pipeline behaviors for cross-cutting concerns

### Domain Events

- Events defined in aggregate `Events/` folders
- Dispatched through `IDomainEventDispatcher`

### Modular Monolith

- Bot and Worker modules as separate concerns
- Shared infrastructure and domain
- Independent scaling potential

## Technology Stack

- **.NET 9**: Primary framework
- **PostgreSQL**: Primary database
- **EF Core**: ORM with Npgsql provider
- **Telegram.Bot**: Telegram integration
- **Quartz.NET**: Scheduled job processing
- **MediatR**: In-process messaging
- **Serilog**: Structured logging
- **.NET Aspire**: Orchestration and monitoring
- **Ardalis.Specification**: Specification pattern implementation

## Key Business Concepts

### Families

- Multiple families with different timezones
- Roles: Admin, Adult, Child
- Invitation system for new members

### Tasks

- **One-time tasks**: Single occurrence
- **Periodic tasks**: Automatically created via Quartz cron schedules
- **TaskPoints**: Difficulty scale (1-4 stars: Easy/Medium/Hard/VeryHard)
- **Templates**: Predefined task configurations

### Spots (Responsibility Objects)

- **Pets**: Cats, dogs, hamsters, parrots (with dynamic mood)
- **Home zones**: Kitchen, bathroom, children's room, hallway
- **Appliances & Services**: Washing machine, dishwasher, refrigerator, finances, documents
- **Mood system**: Dynamic pet mood based on task completion

### Gamification

- Points awarded for task completion
- Leaderboard system
- Achievement notifications
- Pet mood visualization

## Development & Deployment

- **CI/CD**: GitHub Actions with PR previews
- **Docker**: Containerized deployment
- **VPS**: Production deployment on virtual private servers
- **Secrets**: GitHub Secrets for configuration
- **Monitoring**: Health checks and logging

## File Structure Summary

```
FamilyTaskManager/
├── src/
│   ├── FamilyTaskManager.Core/          # Domain layer
│   ├── FamilyTaskManager.UseCases/      # Application layer
│   ├── FamilyTaskManager.Infrastructure/ # Infrastructure layer
│   ├── FamilyTaskManager.Host/          # Modular monolith
│   ├── FamilyTaskManager.AspireHost/    # Aspire orchestration
│   └── FamilyTaskManager.ServiceDefaults/
├── tests/                               # Test projects
├── docs/                                # Documentation
├── docker-compose.yml                   # Docker compose
├── Dockerfile                          # Docker configuration
├── .github/                            # GitHub Actions
└── README.md                           # Project overview
```

## Important Interfaces

### Repository Interfaces

```csharp
// Custom repository with GetOrCreateAndSaveAsync
public interface IAppRepository<T> : IRepository<T> where T : class, IAggregateRoot
{
    Task<T> GetOrCreateAndSaveAsync(
        ISpecification<T> findSpec,
        Func<T> createEntity,
        CancellationToken cancellationToken = default);
}

// Read-only repository
public interface IAppReadRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
}
```

### Domain Services

- `ITaskInstanceFactory`: Creates task instances from templates
- `ISpotMoodCalculator`: Calculates pet mood based on task completion

## Entry Points

1. **Main Application**: `src/FamilyTaskManager.Host/Program.cs`
2. **Aspire Orchestration**: `src/FamilyTaskManager.AspireHost/Program.cs`
3. **Telegram Bot**: `@ChoreHeroesBot` (production bot)

## Development Workflow

1. Local development via AspireHost with Docker
2. PR creation triggers preview deployment
3. Merge to main triggers production deployment
4. Monitoring via health checks and logs

```