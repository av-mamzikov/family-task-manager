# Быстрый старт

Краткое руководство для локального тестирования и деплоя.

## 🧪 Локальное тестирование (5 минут)

### Windows (PowerShell)

```powershell
# 1. Создайте .env файл
copy .env.example .env
notepad .env

# 2. Заполните минимальные данные в .env:
# TELEGRAM_BOT_TOKEN=ваш_токен
# TELEGRAM_BOT_USERNAME=ваш_бот

# 3. Запустите автоматический тест
.\scripts\test-local.ps1
```

### Linux/macOS/WSL

```bash
# 1. Создайте .env файл
cp .env.example .env
nano .env

# 2. Заполните минимальные данные в .env:
# TELEGRAM_BOT_TOKEN=ваш_токен
# TELEGRAM_BOT_USERNAME=ваш_бот

# 3. Запустите автоматический тест
bash scripts/test-local.sh
```

### Ручное тестирование

```bash
# Сборка образа
docker build -t family-task-manager:test .

# Запуск с production конфигурацией
docker tag family-task-manager:test test/family-task-manager:latest
docker compose -f docker-compose.prod.yml up -d

# Проверка логов
docker compose -f docker-compose.prod.yml logs -f

# Остановка
docker compose -f docker-compose.prod.yml down
```

## ✅ CI/CD - Автоматическое тестирование

При каждом push и Pull Request автоматически запускаются:

- ✅ **Tests** - все unit тесты
- ✅ **Code Quality** - проверка форматирования и warnings
- ✅ **Code Coverage** - измерение покрытия кода

**Деплой происходит только если все тесты прошли успешно!**

Подробнее: [CI/CD Pipeline](.github/CI_CD.md)

## 🚀 Деплой на VPS (30 минут)

### 1. Подготовка (5 минут)

- [ ] Зарегистрируйтесь на [Docker Hub](https://hub.docker.com/)
- [ ] Арендуйте VPS (рекомендую [Timeweb Cloud](https://timeweb.cloud/) за 169₽/мес)
- [ ] Получите IP адрес сервера

### 2. Настройка сервера (10 минут)

```bash
# Подключитесь к серверу
ssh root@ваш_ip

# Скопируйте и запустите скрипт настройки
# (на вашем компьютере)
scp scripts/server-setup.sh root@ваш_ip:/tmp/
ssh root@ваш_ip "bash /tmp/server-setup.sh"

# Настройте .env на сервере
ssh root@ваш_ip
nano /opt/family-task-manager/.env
```

Заполните `.env` на сервере:

```env
DOCKER_USERNAME=ваш_dockerhub_username
POSTGRES_USER=familytask
POSTGRES_PASSWORD=сильный_пароль_123
TELEGRAM_BOT_TOKEN=ваш_токен
TELEGRAM_BOT_USERNAME=ваш_бот
```

Скопируйте файлы:

```bash
# На вашем компьютере
scp docker-compose.prod.yml root@ваш_ip:/opt/family-task-manager/docker-compose.yml
scp scripts/init-db.sql root@ваш_ip:/opt/family-task-manager/scripts/
scp scripts/deploy.sh root@ваш_ip:/opt/family-task-manager/
ssh root@ваш_ip "chmod +x /opt/family-task-manager/deploy.sh"
```

### 3. Настройка GitHub (10 минут)

Создайте SSH ключ на сервере:

```bash
ssh root@ваш_ip
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github-actions -N ""
cat ~/.ssh/github-actions.pub >> ~/.ssh/authorized_keys
cat ~/.ssh/github-actions  # Скопируйте этот ключ
```

Добавьте секреты в GitHub (`Settings` → `Secrets and variables` → `Actions`):

| Секрет            | Значение                       |
|-------------------|--------------------------------|
| `DOCKER_USERNAME` | ваш_dockerhub_username         |
| `DOCKER_PASSWORD` | ваш_dockerhub_password         |
| `VPS_HOST`        | IP_адрес_сервера               |
| `VPS_USERNAME`    | root                           |
| `VPS_SSH_KEY`     | приватный ключ из команды выше |

### 4. Первый деплой (5 минут)

```bash
# На вашем компьютере
git add .
git commit -m "Setup deployment"
git push origin main
```

GitHub Actions автоматически задеплоит приложение!

Проверьте статус:

- В GitHub: вкладка `Actions`
- На сервере: `ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"`

## 📋 Полезные команды

### На сервере

```bash
# Подключение
ssh root@ваш_ip

# Статус
cd /opt/family-task-manager && docker compose ps

# Логи
cd /opt/family-task-manager && docker compose logs -f

# Перезапуск
cd /opt/family-task-manager && docker compose restart

# Ручной деплой
cd /opt/family-task-manager && bash deploy.sh
```

### Локально

```bash
# Тестирование
.\scripts\test-local.ps1  # Windows
bash scripts/test-local.sh  # Linux/macOS

# Сборка образа
docker build -t family-task-manager:test .

# Запуск локально
docker compose -f docker-compose.prod.yml up -d

# Остановка
docker compose -f docker-compose.prod.yml down
```

## 🔍 Проверка работы

### После деплоя проверьте:

1. **GitHub Actions**: Зелёная галочка в разделе Actions
2. **Контейнеры на сервере**:
   ```bash
   ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"
   ```
3. **Логи бота**:
   ```bash
   ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs family-task-manager"
   ```
4. **Telegram бот**: Отправьте `/start` боту

## ❓ Проблемы?

### Бот не отвечает

```bash
# Проверьте логи
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs -f family-task-manager"

# Проверьте, что контейнер запущен
ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"

# Перезапустите
ssh root@ваш_ip "docker compose -C /opt/family-task-manager restart"
```

### GitHub Actions не работает

1. Проверьте все секреты в `Settings` → `Secrets and variables` → `Actions`
2. Проверьте логи в разделе `Actions` на GitHub
3. Убедитесь, что SSH ключ правильный

### Контейнер не запускается

```bash
# Проверьте .env файл
ssh root@ваш_ip "cat /opt/family-task-manager/.env"

# Проверьте логи
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs"

# Пересоберите
ssh root@ваш_ip "cd /opt/family-task-manager && docker compose pull && docker compose up -d"
```

## 📚 Подробная документация

- **Локальное тестирование**: [LOCAL_TESTING.md](LOCAL_TESTING.md)
- **Полная инструкция по деплою**: [DEPLOYMENT.md](DEPLOYMENT.md)
- **Настройка Telegram бота**: [TELEGRAM_BOT_SETUP.md](TELEGRAM_BOT_SETUP.md)

## 💰 Стоимость

**Минимальная конфигурация:**

- VPS (Timeweb Cloud 1): **169₽/месяц**
- Docker Hub: **Бесплатно**
- GitHub Actions: **Бесплатно** (2000 минут/месяц)

**Итого: ~169₽/месяц** (~$1.7/месяц)

---

**Готово!** Теперь при каждом пуше в `main` ветку бот автоматически обновляется на сервере. 🎉
