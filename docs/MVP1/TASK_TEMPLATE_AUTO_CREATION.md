# Автоматическое создание TaskTemplate

## Обзор

При создании питомца в системе автоматически создаются шаблоны задач (`TaskTemplate`) в соответствии с типом питомца. Это обеспечивает, что у каждого питомца сразу есть набор периодических задач по уходу.

## Реализация

### 1. PetTaskTemplateData

Статический класс `PetTaskTemplateData` содержит предопределенные шаблоны задач для каждого типа питомца:

**Расположение:** `src/FamilyTaskManager.UseCases/Pets/PetTaskTemplateData.cs`

**Типы питомцев и количество шаблонов:**
- **Кот (Cat):** 8 шаблонов
  - 4 ежедневные задачи (уборка лотка, вода, корм, игра)
  - 4 периодические задачи (замена наполнителя, мытье мисок, чистка места для сна)

- **Собака (Dog):** 6 шаблонов
  - 3 ежедневные задачи (утренняя прогулка, вечерняя прогулка, кормление)
  - 3 периодические задачи (мытье мисок, расчесывание, купание)

- **Хомяк (Hamster):** 5 шаблонов
  - 2 ежедневные задачи (корм, вода)
  - 3 периодические задачи (уборка клетки, полная уборка, проверка игрушек)

### 2. CreatePetHandler

Обработчик команды `CreatePetCommand` был обновлен для автоматического создания шаблонов:

**Расположение:** `src/FamilyTaskManager.UseCases/Pets/CreatePet.cs`

**Изменения:**
1. Добавлен параметр `IRepository<TaskTemplate>` в конструктор
2. После создания питомца автоматически создаются шаблоны задач:
   ```csharp
   var defaultTemplates = PetTaskTemplateData.GetDefaultTemplates(command.Type);
   var systemUserId = Guid.Empty; // System-created templates
   
   foreach (var templateData in defaultTemplates)
   {
     var taskTemplate = new TaskTemplate(
       familyId: command.FamilyId,
       petId: pet.Id,
       title: templateData.Title,
       points: templateData.Points,
       schedule: templateData.Schedule,
       createdBy: systemUserId);
     
     await taskTemplateRepository.AddAsync(taskTemplate, cancellationToken);
   }
   ```

### 3. Системный пользователь

Все автоматически созданные шаблоны имеют `CreatedBy = Guid.Empty`, что обозначает, что они созданы системой, а не конкретным пользователем.

## Расписания (Cron выражения)

Все расписания используют формат Quartz Cron:

- **Ежедневно в 9:00:** `0 0 9 * * ?`
- **Ежедневно в 20:00:** `0 0 20 * * ?`
- **Раз в 2 дня в 19:00:** `0 0 19 */2 * ?`
- **Раз в 7 дней в 12:00:** `0 0 12 */7 * ?`
- **Раз в неделю (воскресенье) в 12:00:** `0 0 12 ? * SUN`
- **Раз в неделю (суббота) в 18:00:** `0 0 18 ? * SAT`
- **Раз в месяц (1-го числа) в 15:00:** `0 0 15 1 * ?`

## Тестирование

### Unit тесты

**CreatePetHandlerTests** (`tests/FamilyTaskManager.UnitTests/UseCases/Pets/CreatePetHandlerTests.cs`):
- `Handle_ValidCommand_CreatesDefaultTaskTemplates` - проверяет количество созданных шаблонов
- `Handle_CreatesCatPet_CreatesCorrectTaskTemplates` - проверяет корректность шаблонов для кота
- `Handle_CreatesDogPet_CreatesCorrectTaskTemplates` - проверяет корректность шаблонов для собаки
- `Handle_CreatesHamsterPet_CreatesCorrectTaskTemplates` - проверяет корректность шаблонов для хомяка

**PetTaskTemplateDataTests** (`tests/FamilyTaskManager.UnitTests/UseCases/Pets/PetTaskTemplateDataTests.cs`):
- Проверяет корректность данных в `PetTaskTemplateData`
- Проверяет валидность расписаний и баллов

## Использование

При создании питомца через бота или API:

```csharp
var command = new CreatePetCommand(
    FamilyId: familyId,
    Type: PetType.Cat,
    Name: "Мурзик"
);

var result = await mediator.Send(command);
// После выполнения команды будет создан питомец и 8 шаблонов задач
```

## Источник данных

Все шаблоны задач основаны на документе: `docs/MVP1/Шаблоны питомцев и задач.md`
