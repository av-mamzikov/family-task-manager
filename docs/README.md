# 📚 Документация Family Task Manager

Полная документация по разработке, настройке и развертыванию Family Task Manager.

## 🚀 Быстрый старт

Начните здесь, если хотите быстро запустить проект:

- **[Быстрый старт](../QUICK_START.md)** - локальное тестирование за 5 минут
- **[Локальное тестирование](../LOCAL_TESTING.md)** - тестирование Docker образа
- **[Host Quick Start](../src/FamilyTaskManager.Host/QUICK_START.md)** - запуск модульного монолита

## 🖥️ Настройка Production

Пошаговые инструкции по развертыванию на VPS:

### Основная настройка

- **[VPS Setup](setup/VPS_SETUP.md)** - настройка сервера (Docker, Registry, SSH)
- **[GitHub Secrets Setup](setup/GITHUB_SECRETS_SETUP.md)** - 🔐 установка секретов в GitHub
- **[GitHub Actions Setup](setup/GITHUB_ACTIONS_SETUP.md)** - автоматический CI/CD
- **[Deployment Summary](../DEPLOYMENT_SUMMARY.md)** - обзор всех вариантов деплоя

### Дополнительные инструменты

- **[Portainer Setup](PORTAINER_SETUP.md)** - Web UI для управления контейнерами
- **[Private Registry Setup](PRIVATE_REGISTRY_SETUP.md)** - собственный Docker Registry
- **[Telegram Bot Setup](TELEGRAM_BOT_SETUP.md)** - создание и настройка бота

## 🏗️ Разработка

Документация для разработчиков:

### Архитектура и структура

- **[Host README](../src/FamilyTaskManager.Host/README.md)** - модульный монолит (Bot + Worker)
- **[Infrastructure Setup](INFRASTRUCTURE_SETUP.md)** - работа с БД и миграциями
- **[Use Cases](USE_CASES.md)** - бизнес-логика приложения

### Проектная документация

- **[Концепция MVP1](MVP1/Концепция%20MVP1.md)** - описание продукта и целей
- **[Техническое задание MVP1](MVP1/ТЗ%20MVP1.md)** - требования и спецификации
- **[Шаблоны питомцев и задач](MVP1/Шаблоны%20питомцев%20и%20задач.md)** - предустановленные задачи

## 📂 Структура документации

```
docs/
├── README.md                          # Этот файл
├── setup/                             # Настройка production
│   ├── VPS_SETUP.md                  # Настройка VPS
│   ├── GITHUB_SECRETS_SETUP.md       # 🔐 Установка секретов в GitHub
│   └── GITHUB_ACTIONS_SETUP.md       # CI/CD через GitHub Actions
├── PORTAINER_SETUP.md                # Web UI для Docker
├── PRIVATE_REGISTRY_SETUP.md         # Docker Registry
├── TELEGRAM_BOT_SETUP.md             # Настройка Telegram бота
├── INFRASTRUCTURE_SETUP.md           # База данных и миграции
├── USE_CASES.md                      # Бизнес-логика
└── MVP1/                             # Проектная документация
    ├── Концепция MVP1.md
    ├── ТЗ MVP1.md
    └── Шаблоны питомцев и задач.md
```

## 🎯 Сценарии использования

### Я хочу запустить проект локально

1. [Быстрый старт](../QUICK_START.md)
2. [Настройка Telegram бота](TELEGRAM_BOT_SETUP.md)
3. [Host Quick Start](../src/FamilyTaskManager.Host/QUICK_START.md)

### Я хочу задеплоить на VPS

1. [VPS Setup](setup/VPS_SETUP.md)
2. [GitHub Secrets Setup](setup/GITHUB_SECRETS_SETUP.md)
3. [GitHub Actions Setup](setup/GITHUB_ACTIONS_SETUP.md)
4. [Portainer Setup](PORTAINER_SETUP.md) (опционально)

### Я хочу понять архитектуру

1. [Host README](../src/FamilyTaskManager.Host/README.md)
2. [Use Cases](USE_CASES.md)
3. [Техническое задание MVP1](MVP1/ТЗ%20MVP1.md)

### Я хочу внести изменения

1. [Техническое задание MVP1](MVP1/ТЗ%20MVP1.md)
2. [Use Cases](USE_CASES.md)
3. [Infrastructure Setup](INFRASTRUCTURE_SETUP.md)

## 🔗 Внешние ресурсы

### Технологии

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Quartz.NET](https://www.quartz-scheduler.net/)
- [Docker Documentation](https://docs.docker.com/)

### Инструменты

- [GitHub Actions](https://docs.github.com/en/actions)
- [Portainer](https://docs.portainer.io/)
- [PostgreSQL](https://www.postgresql.org/docs/)

## 💡 Полезные команды

### Разработка

```bash
# Запуск приложения
dotnet run --project src/FamilyTaskManager.Host

# Запуск тестов
dotnet test

# Создание миграции
dotnet ef migrations add MigrationName --project src/FamilyTaskManager.Infrastructure
```

### Docker

```bash
# Локальная сборка
docker-compose up -d

# Просмотр логов
docker-compose logs -f

# Остановка
docker-compose down
```

### Production

```bash
# Подключение к VPS
ssh root@ваш_ip

# Просмотр статуса
docker compose -f docker-compose.prod.yml ps

# Просмотр логов
docker compose -f docker-compose.prod.yml logs -f
```

## 🆘 Помощь и поддержка

Если вы столкнулись с проблемами:

1. Проверьте [Troubleshooting разделы](setup/VPS_SETUP.md#troubleshooting) в документации
2. Посмотрите [Issues на GitHub](https://github.com/av-mamzikov/family-task-manager/issues)
3. Создайте новый Issue с описанием проблемы

## 📝 Вклад в документацию

Документация - живой организм. Если вы нашли ошибку или хотите что-то улучшить:

1. Создайте Issue с описанием проблемы
2. Или сразу создайте Pull Request с исправлениями
3. Следуйте структуре существующих документов

---

**Последнее обновление:** 24 ноября 2025
