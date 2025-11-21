# Infrastructure Layer Setup

## Реализованные компоненты

### 1. EF Core Конфигурации

Созданы конфигурации для всех доменных сущностей:

- **UserConfiguration** - конфигурация для User с уникальным индексом по TelegramId
- **FamilyConfiguration** - конфигурация для Family с настройками по умолчанию
- **FamilyMemberConfiguration** - конфигурация для FamilyMember с композитными индексами
- **PetConfiguration** - конфигурация для Pet с индексом по FamilyId
- **TaskTemplateConfiguration** - конфигурация для TaskTemplate с множественными индексами
- **TaskInstanceConfiguration** - конфигурация для TaskInstance с индексами для оптимизации запросов
- **ActionHistoryConfiguration** - конфигурация для ActionHistory с JSONB полем для метаданных

### 2. AppDbContext

Добавлены DbSets для всех сущностей:
- Users
- Families
- FamilyMembers
- Pets
- TaskTemplates
- TaskInstances
- ActionHistory

### 3. Миграции

Создана начальная миграция `InitialDomainModel` с:
- Всеми таблицами для доменной модели
- Индексами для оптимизации запросов
- Foreign keys для связей
- Default values для полей
- PostgreSQL-специфичными типами (uuid, jsonb, timestamp with time zone)

### 4. Design-Time DbContext Factory

Создан `AppDbContextFactory` для поддержки EF Core миграций в design-time режиме.

## База данных

Проект настроен на использование **PostgreSQL** в качестве основной БД.

### Версии пакетов

- **Npgsql.EntityFrameworkCore.PostgreSQL**: 10.0.0-rc.2
- **Microsoft.EntityFrameworkCore**: 10.0.0

> ⚠️ **Примечание**: Используется RC версия Npgsql для совместимости с .NET 10 и EF Core 10.0. При сборке будут предупреждения о несовпадении версий, но это ожидаемое поведение до выхода стабильной версии Npgsql 10.0.0.

## Применение миграций

Для применения миграций к БД:

```bash
dotnet ef database update --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext
```

## Работа с миграциями

### Создание новой миграции

После изменения доменной модели или EF Core конфигураций:

1. **Сначала соберите Infrastructure проект** (с игнорированием предупреждений о версиях):
   ```bash
   dotnet build src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj -p:TreatWarningsAsErrors=false
   ```

2. **Создайте миграцию**:
   ```bash
   dotnet ef migrations add <MigrationName> --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext --no-build
   ```
   
   Примеры названий миграций:
   - `AddInviteCodeToFamily`
   - `AddDueAtToTaskInstance`
   - `UpdatePetMoodCalculation`

3. **Переместите файлы миграции** в правильную папку (если они создались в корне):
   ```powershell
   Move-Item -Path "src\FamilyTaskManager.Infrastructure\Migrations\*" -Destination "src\FamilyTaskManager.Infrastructure\Data\Migrations\" -Force
   ```

4. **Обновите namespace** в созданных файлах:
   - Замените `namespace FamilyTaskManager.Infrastructure.Migrations` на `namespace FamilyTaskManager.Infrastructure.Data.Migrations`

### Удаление последней миграции

Если миграция была создана ошибочно:

```bash
dotnet ef migrations remove --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext --force
```

### Просмотр списка миграций

```bash
dotnet ef migrations list --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext
```

### Генерация SQL скрипта

Для просмотра SQL, который будет выполнен:

```bash
dotnet ef migrations script --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext --output migration.sql
```

Для конкретной миграции:
```bash
dotnet ef migrations script <FromMigration> <ToMigration> --project src\FamilyTaskManager.Infrastructure\FamilyTaskManager.Infrastructure.csproj --context AppDbContext --output migration.sql
```

## Connection String

Connection string для PostgreSQL настраивается через:
- **Design-time**: в `AppDbContextFactory` (по умолчанию localhost)
- **Runtime**: через appsettings.json или .NET Aspire

Пример connection string:
```
Host=localhost;Database=FamilyTaskManager;Username=postgres;Password=postgres
```

## Следующие шаги

1. Настроить PostgreSQL через .NET Aspire
2. Реализовать Use Cases для работы с доменной моделью
3. Настроить Telegram Bot для взаимодействия с пользователями
4. Реализовать Quartz Worker для периодических задач
