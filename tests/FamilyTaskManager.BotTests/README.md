# Тесты Telegram Bot

Этот проект содержит unit-тесты для Telegram Bot компонента Family Task Manager.

## Структура тестов

### Models
- `UserSessionTests` - тесты модели сессии пользователя

### Services
- `SessionManagerTests` - тесты менеджера сессий

### Handlers/Commands
- `FamilyCommandHandlerTests` - тесты обработчика /family
- `TasksCommandHandlerTests` - тесты обработчика /tasks

## Запуск тестов

```bash
# Запуск всех тестов
dotnet test

# С покрытием кода
dotnet test /p:CollectCoverage=true
```

## Используемые библиотеки

- **xUnit** - фреймворк для тестирования
- **NSubstitute** - мокирование зависимостей
- **Shouldly** - fluent assertions
