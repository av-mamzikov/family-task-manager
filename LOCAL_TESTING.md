# Локальное тестирование деплоя

Руководство по тестированию всей системы деплоя на локальном компьютере перед развёртыванием на VPS.

## Содержание

- [Тестирование Docker образа](#тестирование-docker-образа)
- [Тестирование production конфигурации](#тестирование-production-конфигурации)
- [Тестирование GitHub Actions локально](#тестирование-github-actions-локально)
- [Проверка всех компонентов](#проверка-всех-компонентов)

## Тестирование Docker образа

### 1. Сборка образа локально

```bash
# Перейдите в корень проекта
cd C:\Users\avmam\source\family-tak-manager\family-tak-manager

# Соберите Docker образ
docker build -t family-task-manager:test .
```

**Ожидаемый результат**: Образ успешно собран без ошибок.

### 2. Проверка размера образа

```bash
docker images family-task-manager:test
```

**Хороший размер**: 200-300 MB для .NET 9 приложения.

### 3. Запуск контейнера для проверки

```bash
# Создайте тестовый .env файл
copy .env.example .env.test

# Отредактируйте .env.test и укажите ваши данные
notepad .env.test
```

Содержимое `.env.test`:

```env
TELEGRAM_BOT_TOKEN=ваш_тестовый_токен
TELEGRAM_BOT_USERNAME=ваш_бот
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
```

Запустите контейнер:

```bash
docker run --rm -it ^
  --env-file .env.test ^
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=postgres123" ^
  family-task-manager:test
```

**Ожидаемый результат**: Приложение запускается без ошибок (может упасть из-за отсутствия БД, это нормально).

## Тестирование production конфигурации

### 1. Подготовка окружения

Создайте `.env` файл в корне проекта:

```bash
copy .env.example .env
notepad .env
```

Содержимое:

```env
DOCKER_USERNAME=test
POSTGRES_USER=familytask
POSTGRES_PASSWORD=test_password_123
TELEGRAM_BOT_TOKEN=ваш_токен
TELEGRAM_BOT_USERNAME=ваш_бот
```

### 2. Запуск с production конфигурацией

```bash
# Сначала соберите образ с тегом как в production
docker build -t test/family-task-manager:latest .

# Запустите с production конфигурацией
docker compose -f docker-compose.prod.yml up -d
```

### 3. Проверка статуса

```bash
# Проверьте, что все контейнеры запущены
docker compose -f docker-compose.prod.yml ps

# Проверьте логи бота
docker compose -f docker-compose.prod.yml logs -f family-task-manager

# Проверьте логи БД
docker compose -f docker-compose.prod.yml logs postgres
```

### 4. Тестирование бота

Отправьте команду `/start` вашему боту в Telegram.

**Ожидаемый результат**: Бот отвечает корректно.

### 5. Остановка

```bash
docker compose -f docker-compose.prod.yml down
```

## Тестирование GitHub Actions локально

### 1. Установка act

`act` позволяет запускать GitHub Actions локально.

**Windows (через Chocolatey):**

```bash
choco install act-cli
```

**Или скачайте с**: https://github.com/nektos/act/releases

### 2. Настройка секретов для act

Создайте файл `.secrets` в корне проекта:

```env
DOCKER_USERNAME=ваш_dockerhub_username
DOCKER_PASSWORD=ваш_dockerhub_password
VPS_HOST=127.0.0.1
VPS_USERNAME=test
VPS_SSH_KEY=test_key
```

**Важно**: Добавьте `.secrets` в `.gitignore`!

### 3. Запуск workflow локально

```bash
# Проверка синтаксиса workflow
act -l

# Запуск только этапа сборки (без деплоя)
act push --secret-file .secrets -j build-and-deploy --dry-run

# Полный запуск (будет пытаться задеплоить)
act push --secret-file .secrets
```

**Примечание**: Этап деплоя на SSH не сработает локально, но сборка образа будет протестирована.

## Проверка всех компонентов

### Чеклист перед деплоем:

#### ✅ Docker образ

- [ ] Образ собирается без ошибок
- [ ] Размер образа разумный (< 500 MB)
- [ ] Приложение запускается в контейнере

#### ✅ Production конфигурация

- [ ] `docker-compose.prod.yml` корректен
- [ ] Все переменные окружения настроены
- [ ] PostgreSQL запускается и проходит healthcheck
- [ ] Бот подключается к БД
- [ ] Бот отвечает на команды

#### ✅ GitHub Actions

- [ ] Workflow файл валиден (проверьте в GitHub)
- [ ] Все необходимые секреты определены
- [ ] Сборка образа работает локально

#### ✅ Скрипты

- [ ] `server-setup.sh` имеет корректный синтаксис
- [ ] `deploy.sh` имеет корректный синтаксис

### Проверка синтаксиса скриптов

**Linux/WSL:**

```bash
# Проверка синтаксиса bash скриптов
bash -n scripts/server-setup.sh
bash -n scripts/deploy.sh
```

**Windows (Git Bash):**

```bash
bash -n scripts/server-setup.sh
bash -n scripts/deploy.sh
```

## Быстрый тест всей системы

Используйте этот скрипт для быстрой проверки:

```bash
# 1. Сборка образа
docker build -t family-task-manager:test .

# 2. Проверка размера
docker images family-task-manager:test

# 3. Запуск с production конфигурацией
docker compose -f docker-compose.prod.yml up -d

# 4. Проверка статуса
timeout /t 10
docker compose -f docker-compose.prod.yml ps

# 5. Проверка логов
docker compose -f docker-compose.prod.yml logs family-task-manager

# 6. Тест бота (отправьте /start в Telegram)
echo "Отправьте /start боту в Telegram"
pause

# 7. Остановка
docker compose -f docker-compose.prod.yml down
```

## Тестирование миграций БД

### 1. Запуск только БД

```bash
docker compose -f docker-compose.prod.yml up -d postgres
```

### 2. Проверка подключения

```bash
docker exec -it family-task-postgres psql -U familytask -d FamilyTaskManager
```

Выполните в psql:

```sql
-- Проверка таблиц
\dt

-- Проверка данных
SELECT *
FROM "Families" LIMIT 5;

-- Выход
\q
```

### 3. Остановка

```bash
docker compose -f docker-compose.prod.yml down
```

## Симуляция production окружения

Для максимально точного тестирования:

### 1. Используйте production переменные

В `.env` используйте те же значения, что будут на production (кроме токенов).

### 2. Тестируйте с production образом

```bash
# Соберите с тем же тегом, что будет на production
docker build -t ваш_dockerhub_username/family-task-manager:latest .

# Запустите
docker compose -f docker-compose.prod.yml up -d
```

### 3. Проверьте логирование

```bash
# Проверьте, что логи ротируются (настройка в docker-compose.prod.yml)
docker inspect family-task-manager | grep -A 10 LogConfig
```

### 4. Проверьте автоперезапуск

```bash
# Остановите контейнер
docker stop family-task-manager

# Подождите несколько секунд
timeout /t 5

# Проверьте, что он перезапустился
docker ps | findstr family-task-manager
```

## Troubleshooting локального тестирования

### Проблема: Docker образ не собирается

**Решение**:

```bash
# Очистите кэш Docker
docker builder prune -a

# Попробуйте снова
docker build --no-cache -t family-task-manager:test .
```

### Проблема: Контейнер сразу останавливается

**Решение**:

```bash
# Посмотрите логи
docker logs family-task-manager

# Запустите в интерактивном режиме
docker run --rm -it family-task-manager:test
```

### Проблема: Не могу подключиться к БД

**Решение**:

```bash
# Проверьте, что PostgreSQL запущен
docker compose -f docker-compose.prod.yml ps postgres

# Проверьте логи БД
docker compose -f docker-compose.prod.yml logs postgres

# Проверьте healthcheck
docker inspect family-task-postgres | grep -A 5 Health
```

### Проблема: act не работает на Windows

**Решение**:

- Используйте WSL2 для запуска act
- Или используйте Docker Desktop с WSL2 backend
- Или пропустите тестирование GitHub Actions локально

## Финальная проверка

Перед деплоем на VPS убедитесь:

1. ✅ Образ собирается без ошибок
2. ✅ Приложение работает локально с `docker-compose.prod.yml`
3. ✅ Бот отвечает на команды
4. ✅ БД сохраняет данные
5. ✅ Логи пишутся корректно
6. ✅ Контейнеры автоматически перезапускаются

**Готово!** Теперь можно безопасно деплоить на VPS.

---

## Дополнительные команды

### Очистка после тестирования

```bash
# Остановить все контейнеры
docker compose -f docker-compose.prod.yml down -v

# Удалить тестовые образы
docker rmi family-task-manager:test

# Очистить неиспользуемые ресурсы
docker system prune -a
```

### Мониторинг ресурсов

```bash
# Использование CPU/RAM
docker stats

# Размер образов
docker images

# Размер volumes
docker system df
```
