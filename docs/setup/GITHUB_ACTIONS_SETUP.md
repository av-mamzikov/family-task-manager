# 🤖 GitHub Actions Setup

Настройка автоматического CI/CD через GitHub Actions для Family Task Manager.

## Обзор

GitHub Actions автоматически:

1. ✅ Запускает тесты при каждом push
2. ✅ Собирает Docker образ
3. ✅ Отправляет образ в Private Registry на VPS
4. ✅ Деплоит приложение на VPS

## Workflows

### 1. Tests (`tests.yml`)

Запускается при:

- Push в `main`, `master`, `develop`
- Pull Request в эти ветки

**Что делает:**

- Запускает PostgreSQL в service container
- Выполняет `dotnet test`
- Генерирует отчёт о тестировании
- Загружает результаты как artifacts

### 2. Deploy (`deploy-registry.yml`)

Запускается при:

- Push в `main`/`master` (после успешных тестов)
- Ручной запуск (`workflow_dispatch`)
- PR с label `deploy-preview`

**Что делает:**

- Определяет окружение (production/pr-preview)
- Собирает Docker образ
- Отправляет в Private Registry через SSH tunnel
- Копирует `docker-compose.prod.yml` на VPS
- Деплоит через SSH

## Настройка Secrets

### Обязательные секреты

Перейдите: `Settings` → `Secrets and variables` → `Actions`

#### VPS и Registry

```
VPS_HOST              # IP адрес VPS (например: 123.45.67.89)
VPS_USERNAME          # SSH username (обычно: root)
VPS_SSH_KEY           # Приватный SSH ключ (из ssh-keygen)
REGISTRY_USERNAME     # Username для Docker Registry
REGISTRY_PASSWORD     # Пароль для Docker Registry
```

#### Production окружение

```
TELEGRAM_BOT_TOKEN    # Токен production бота от @BotFather
TELEGRAM_BOT_USERNAME # Username бота (без @)
POSTGRES_USER         # PostgreSQL user (например: familytask)
POSTGRES_PASSWORD     # Сильный пароль для PostgreSQL
```

#### PR Preview (опционально)

```
PR_BOT_TOKEN          # Токен тестового бота
PR_BOT_USERNAME       # Username тестового бота
PR_POSTGRES_USER      # familytask_pr
PR_POSTGRES_PASSWORD  # Пароль для тестовой БД
```

## Использование

### Автоматический деплой в production

```bash
# Просто запушьте в main
git add .
git commit -m "Your changes"
git push origin main
```

**Процесс:**

1. Запускаются тесты (`tests.yml`)
2. Если тесты успешны → запускается деплой (`deploy-registry.yml`)
3. Приложение автоматически обновляется на VPS

### Ручной деплой

1. Перейдите в **Actions** → **Deploy to VPS**
2. Нажмите **"Run workflow"**
3. Выберите:
    - **Branch:** ветку для деплоя
    - **Environment:** `production` или `pr-preview`
    - **PR number:** (только для pr-preview)
4. Нажмите **"Run workflow"**

### PR Preview деплой

**Вариант 1: Через label**

1. Создайте PR
2. Добавьте label `deploy-preview`
3. Автоматически запустится деплой в изолированное окружение

**Вариант 2: Вручную**

1. Создайте PR
2. Actions → Deploy to VPS → Run workflow
3. Выберите PR ветку и `pr-preview` environment
4. Укажите номер PR

**Что получите:**

- Отдельный Telegram бот
- Отдельная база данных
- Независимые контейнеры
- Не влияет на production

## Структура workflows

### Setup Job

Определяет параметры окружения:

- `is_production` / `is_pr_preview`
- `image_tag` (latest или pr-123)
- `deploy_dir` (/opt/family-task-manager или /opt/family-task-manager-pr-123)
- Секреты для окружения

### Build-and-Push Job

1. Checkout кода
2. Setup Docker Buildx
3. SSH tunnel к registry на VPS
4. Login в registry
5. Build образа с метаданными
6. Push образа с тегами:
    - `latest` (production)
    - `latest-abc123` (с commit hash)
    - `main` (по имени ветки)

### Deploy Job

1. Checkout кода
2. Копирование `docker-compose.prod.yml` на VPS
3. SSH подключение к VPS
4. Создание/обновление `.env` файла
5. Pull нового образа из registry
6. Backup БД (только production)
7. Остановка старых контейнеров
8. Запуск новых контейнеров
9. Проверка статуса и логов

## Условия запуска

### Tests workflow

- ✅ Всегда при push/PR
- ✅ Независимо от других workflows

### Deploy workflow

- ✅ `workflow_dispatch` - всегда (ручной запуск)
- ✅ `pull_request` с label `deploy-preview` - всегда
- ✅ `workflow_run` - только если тесты успешны

## Мониторинг

### Просмотр логов workflow

1. Перейдите в **Actions**
2. Выберите workflow run
3. Кликните на job для просмотра логов

### Проверка деплоя

```bash
# На VPS
ssh root@ваш_ip
cd /opt/family-task-manager
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f
```

### Badges в README

```markdown
[![Tests](https://github.com/username/repo/actions/workflows/tests.yml/badge.svg)](https://github.com/username/repo/actions/workflows/tests.yml)
[![Deploy](https://github.com/username/repo/actions/workflows/deploy-registry.yml/badge.svg)](https://github.com/username/repo/actions/workflows/deploy-registry.yml)
```

## Troubleshooting

### Тесты падают

```bash
# Локально запустите тесты
dotnet test

# Проверьте логи в GitHub Actions
# Actions → Tests → View logs
```

### Деплой не запускается

**Проверьте:**

- ✅ Тесты прошли успешно
- ✅ Все секреты настроены
- ✅ VPS доступен по SSH
- ✅ Registry работает на VPS

### SSH подключение не работает

```bash
# Проверьте SSH ключ в секретах
# Он должен быть приватным ключом (начинается с -----BEGIN OPENSSH PRIVATE KEY-----)

# Проверьте публичный ключ на VPS
ssh root@ваш_ip
cat ~/.ssh/authorized_keys
```

### Registry недоступен

```bash
# На VPS проверьте registry
docker ps | grep registry
docker logs registry

# Перезапустите registry
cd /opt/docker-registry
docker compose restart
```

### Образ не собирается

**Проверьте:**

- ✅ Dockerfile существует в корне
- ✅ Все зависимости доступны
- ✅ Нет ошибок в коде

## Оптимизация

### Кэширование

GitHub Actions автоматически кэширует:

- Docker layers (через Buildx)
- NuGet packages (можно добавить)

### Параллельные jobs

Тесты и сборка могут идти параллельно, но деплой ждёт успешных тестов.

### Secrets rotation

Регулярно обновляйте:

- SSH ключи (раз в 6-12 месяцев)
- Registry пароли (раз в 3-6 месяцев)
- Bot tokens (при необходимости)

## Расширенная настройка

### Добавление environments

В GitHub можно создать environments с защитой:

1. `Settings` → `Environments` → `New environment`
2. Создайте `production` environment
3. Добавьте protection rules:
    - Required reviewers
    - Wait timer
    - Deployment branches

### Notifications

Настройте уведомления о деплоях:

- Slack integration
- Discord webhook
- Email notifications

### Rollback

Для отката к предыдущей версии:

```bash
# На VPS
cd /opt/family-task-manager
docker pull localhost:5000/family-task-manager:latest-<old_commit_hash>
docker tag localhost:5000/family-task-manager:latest-<old_commit_hash> localhost:5000/family-task-manager:latest
docker compose up -d
```

## Стоимость

**GitHub Actions бесплатно:**

- 2000 минут/месяц для private репозиториев
- Unlimited для public репозиториев

**Ваш проект использует:**

- ~5 минут на тесты
- ~10 минут на деплой
- ~15 минут на полный цикл

**Итого:** ~20 деплоев в месяц бесплатно

## Следующие шаги

- 📖 [VPS Setup](VPS_SETUP.md)
- 🐳 [Portainer Setup](../PORTAINER_SETUP.md)
- 🔒 [Private Registry Setup](../PRIVATE_REGISTRY_SETUP.md)
- 🚀 [Deployment Summary](../../DEPLOYMENT_SUMMARY.md)
