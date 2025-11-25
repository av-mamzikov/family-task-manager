# 🖥️ Локальная разработка через AspireHost

Подробная инструкция по запуску проекта на локальной машине разработчика через .NET Aspire.

## 📋 Требования

- **.NET 9.0+ SDK** ([скачать](https://dotnet.microsoft.com/download))
- **Docker Desktop** ([скачать](https://www.docker.com/products/docker-desktop))
- **Telegram Bot Token** (получить у [@BotFather](https://t.me/BotFather))

---

## 🚀 Быстрый старт (5 минут)

### 1. Клонируйте репозиторий

```bash
git clone <repository-url>
cd family-task-manager
```

### 2. Создайте Telegram бота

1. Откройте [@BotFather](https://t.me/BotFather) в Telegram
2. Отправьте команду `/newbot`
3. Следуйте инструкциям для создания бота
4. Сохраните полученный **Bot Token** (формат: `1234567890:ABCdef...`)
5. Сохраните **Username** бота (без символа @)

> 📖 **Подробнее:** [Telegram Bot Setup](../TELEGRAM_BOT_SETUP.md)

### 3. Настройте секреты для разработки

Проект использует **User Secrets** для хранения конфиденциальных данных в локальной разработке.

**Обязательные секреты:**

```bash
cd src/FamilyTaskManager.AspireHost

# Токен Telegram бота
dotnet user-secrets set "Bot:BotToken" "YOUR_BOT_TOKEN"

# Username бота (без @)
dotnet user-secrets set "Bot:BotUsername" "your_bot_username"
```

**Пример:**

```bash
dotnet user-secrets set "Bot:BotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
dotnet user-secrets set "Bot:BotUsername" "MyFamilyTaskBot"
```

> 💡 **Примечание:** User Secrets хранятся локально на вашей машине и не попадают в Git. Они находятся в:
> - **Windows:** `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
> - **Linux/Mac:** `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### 4. Запустите приложение

**Через командную строку:**

```bash
# Из корня репозитория
cd src/FamilyTaskManager.AspireHost
dotnet run
```

**Через IDE:**

- **Visual Studio:** Установите `FamilyTaskManager.AspireHost` как стартовый проект и нажмите F5
- **Rider:** Выберите конфигурацию запуска `AspireHost` и нажмите Run

### 5. Откройте Aspire Dashboard

После запуска в консоли отобразится URL (обычно `http://localhost:15000` или `https://localhost:17000`).

**В Dashboard вы увидите:**

- 📊 **Ресурсы:** PostgreSQL, pgAdmin, Host
- 📝 **Логи:** в реальном времени от всех компонентов
- 📈 **Метрики:** производительность и здоровье сервисов
- 🔍 **Traces:** распределённая трассировка запросов

### 6. Проверьте работу

1. **Откройте бота в Telegram** и отправьте `/start`
2. **Проверьте pgAdmin:** `http://localhost:5050`
    - Email: `admin@familytask.com`
    - Пароль: `admin123`
3. **Проверьте логи** в Aspire Dashboard

---

## ✅ Что запустилось автоматически

При запуске через AspireHost автоматически поднимаются:

- ✅ **PostgreSQL** контейнер (порт 5432)
- ✅ **pgAdmin** веб-интерфейс (порт 5050)
- ✅ **Telegram Bot** (Long Polling режим)
- ✅ **Quartz Worker** (3 фоновых задачи)
- ✅ **Автоматические миграции БД** (EF Core)
- ✅ **Aspire Dashboard** для мониторинга

---

## 🔧 Управление секретами

### Просмотр всех секретов

```bash
cd src/FamilyTaskManager.AspireHost
dotnet user-secrets list
```

**Вывод:**

```
Bot:BotToken = 1234567890:ABCdefGHIjklMNOpqrsTUVwxyz
Bot:BotUsername = MyFamilyTaskBot
```

### Изменение секрета

```bash
dotnet user-secrets set "Bot:BotToken" "NEW_TOKEN"
```

### Удаление секрета

```bash
dotnet user-secrets remove "Bot:BotToken"
```

### Очистка всех секретов

```bash
dotnet user-secrets clear
```

---

## ⚙️ Дополнительная конфигурация

### Изменение порта PostgreSQL

Если порт 5432 занят, измените его в `Program.cs` AspireHost:

```csharp
var postgres = builder.AddPostgres("postgres")
  .WithEndpoint(port: 5433) // Ваш порт
  .WithLifetime(ContainerLifetime.Persistent);
```

### Изменение порта pgAdmin

```csharp
var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4")
  .WithHttpEndpoint(targetPort: 80, port: 5051) // Ваш порт
  // ...
```

### Отключение pgAdmin

Если pgAdmin не нужен, закомментируйте соответствующий блок в `Program.cs`:

```csharp
// var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4")
//   ...
```

---

## 🐛 Troubleshooting

### Ошибка: "Bot token is invalid"

**Причина:** Неверный или неполный токен бота.

**Решение:**

1. Проверьте токен у @BotFather: отправьте `/token`
2. Убедитесь, что токен скопирован полностью
3. Проверьте отсутствие лишних пробелов

```bash
# Проверьте текущее значение
cd src/FamilyTaskManager.AspireHost
dotnet user-secrets list
```

### Ошибка: "Docker is not running"

**Причина:** Docker Desktop не запущен.

**Решение:**

1. Запустите Docker Desktop
2. Дождитесь полной загрузки
3. Убедитесь, что Docker daemon работает:
   ```bash
   docker ps
   ```

### Ошибка: "Port 5432 is already in use"

**Причина:** Порт PostgreSQL занят другим процессом.

**Решение:**

**Вариант 1:** Остановите другие PostgreSQL инстансы

```bash
# Windows
Get-Process -Name postgres | Stop-Process

# Linux/Mac
sudo systemctl stop postgresql
```

**Вариант 2:** Измените порт в `Program.cs` AspireHost (см. выше)

### Ошибка: "Port 5050 is already in use"

**Причина:** Порт pgAdmin занят.

**Решение:**

1. Измените порт pgAdmin в `Program.cs` (см. выше)
2. Или остановите процесс, занимающий порт 5050

### База данных не создаётся

**Причина:** Проблемы с миграциями или контейнером PostgreSQL.

**Решение:**

1. Проверьте логи в Aspire Dashboard
2. Убедитесь, что PostgreSQL контейнер запущен:
   ```bash
   docker ps | grep postgres
   ```
3. Проверьте миграции:
   ```bash
   cd src/FamilyTaskManager.Infrastructure
   dotnet ef migrations list
   ```
4. Примените миграции вручную:
   ```bash
   dotnet ef database update
   ```

### Ошибка: "User secrets not found"

**Причина:** Секреты не настроены.

**Решение:**

```bash
cd src/FamilyTaskManager.AspireHost
dotnet user-secrets set "Bot:BotToken" "YOUR_TOKEN"
dotnet user-secrets set "Bot:BotUsername" "YOUR_USERNAME"
```

### Aspire Dashboard не открывается

**Причина:** Порт занят или проблемы с запуском.

**Решение:**

1. Проверьте консольный вывод на наличие ошибок
2. Попробуйте другой порт:
   ```bash
   dotnet run --urls "http://localhost:16000"
   ```

---

## 🔍 Полезные команды

### Проверка статуса Docker

```bash
# Проверить запущенные контейнеры
docker ps

# Проверить все контейнеры (включая остановленные)
docker ps -a

# Проверить логи контейнера
docker logs <container_id>
```

### Очистка Docker ресурсов

```bash
# Остановить все контейнеры
docker stop $(docker ps -q)

# Удалить все остановленные контейнеры
docker container prune

# Удалить неиспользуемые volumes
docker volume prune
```

### Проверка .NET SDK

```bash
# Версия SDK
dotnet --version

# Список установленных SDK
dotnet --list-sdks

# Список установленных runtimes
dotnet --list-runtimes
```

### Работа с миграциями

```bash
cd src/FamilyTaskManager.Infrastructure

# Список миграций
dotnet ef migrations list

# Создать новую миграцию
dotnet ef migrations add MigrationName

# Применить миграции
dotnet ef database update

# Откатить миграцию
dotnet ef database update PreviousMigrationName
```

---

## 📊 Мониторинг и отладка

### Aspire Dashboard

**URL:** `http://localhost:15000` (или указанный при запуске)

**Возможности:**

- **Resources:** Статус всех ресурсов (PostgreSQL, pgAdmin, Host)
- **Console Logs:** Логи в реальном времени
- **Structured Logs:** Фильтрация и поиск по логам
- **Traces:** Распределённая трассировка запросов
- **Metrics:** Графики производительности

### pgAdmin

**URL:** `http://localhost:5050`

**Учётные данные:**

- Email: `admin@familytask.com`
- Пароль: `admin123`

**Подключение к БД:**

- Host: `postgres` (имя контейнера)
- Port: `5432`
- Database: `FamilyTaskManager`
- Username: `postgres`
- Password: `postgres`

### Логи приложения

Логи пишутся в консоль и доступны в Aspire Dashboard.

**Уровни логирования** настраиваются в `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Quartz": "Information"
    }
  }
}
```

---

## 🧪 Тестирование

### Запуск тестов

```bash
# Все тесты
dotnet test

# С покрытием кода
dotnet test /p:CollectCoverage=true

# Конкретный проект
dotnet test tests/FamilyTaskManager.UnitTests
```

### Интеграционные тесты

Интеграционные тесты автоматически поднимают Testcontainers с PostgreSQL:

```bash
cd tests/FamilyTaskManager.IntegrationTests
dotnet test
```

---

## 🔐 Безопасность

### ✅ Хорошие практики

- **User Secrets** для локальной разработки (не попадают в Git)
- **Разные боты** для разработки и production
- **Не коммитьте** `.env` файлы с реальными данными
- **Используйте** `.env.example` как шаблон

### ❌ Плохие практики

- ❌ Не храните токены в `appsettings.json`
- ❌ Не коммитьте секреты в Git
- ❌ Не используйте production бота для разработки

---

## 📚 Дополнительные ресурсы

### Документация проекта

- [README](../../README.md) - Общая информация о проекте
- [Telegram Bot Setup](../TELEGRAM_BOT_SETUP.md) - Создание и настройка бота
- [VPS Setup](VPS_SETUP.md) - Настройка production сервера
- [Secrets Setup](SECRETS_SETUP.md) - Настройка секретов для CI/CD

### Внешние ресурсы

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [EF Core Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Quartz.NET Documentation](https://www.quartz-scheduler.net/documentation/)

---

**Время настройки:** ~5-10 минут

**Поддержка:** Если возникли проблемы, создайте Issue в GitHub
