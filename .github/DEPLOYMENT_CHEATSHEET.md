# Шпаргалка по деплою

Быстрый справочник команд для работы с деплоем.

## 🧪 Локальное тестирование

### Автоматический тест

```powershell
# Windows
.\scripts\test-local.ps1
```

```bash
# Linux/macOS
bash scripts/test-local.sh
```

### Ручной тест

```bash
# Сборка
docker build -t family-task-manager:test .

# Запуск
docker tag family-task-manager:test test/family-task-manager:latest
docker compose -f docker-compose.prod.yml up -d

# Логи
docker compose -f docker-compose.prod.yml logs -f

# Остановка
docker compose -f docker-compose.prod.yml down
```

## 🚀 Первоначальная настройка VPS

### 1. Подключение

```bash
ssh root@ваш_ip
```

### 2. Автоматическая настройка

```bash
# На локальном компьютере
scp scripts/server-setup.sh root@ваш_ip:/tmp/
ssh root@ваш_ip "bash /tmp/server-setup.sh"
```

### 3. Настройка .env на сервере

```bash
ssh root@ваш_ip
nano /opt/family-task-manager/.env
```

```env
DOCKER_USERNAME=ваш_dockerhub_username
POSTGRES_USER=familytask
POSTGRES_PASSWORD=сильный_пароль
TELEGRAM_BOT_TOKEN=ваш_токен
TELEGRAM_BOT_USERNAME=ваш_бот
```

### 4. Копирование файлов

```bash
scp docker-compose.prod.yml root@ваш_ip:/opt/family-task-manager/docker-compose.yml
scp scripts/init-db.sql root@ваш_ip:/opt/family-task-manager/scripts/
scp scripts/deploy.sh root@ваш_ip:/opt/family-task-manager/
ssh root@ваш_ip "chmod +x /opt/family-task-manager/deploy.sh"
```

### 5. Создание SSH ключа для GitHub Actions

```bash
ssh root@ваш_ip
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github-actions -N ""
cat ~/.ssh/github-actions.pub >> ~/.ssh/authorized_keys
cat ~/.ssh/github-actions  # Скопируйте для GitHub Secrets
```

## ⚙️ GitHub Secrets

Добавьте в `Settings` → `Secrets and variables` → `Actions`:

| Секрет            | Значение               |
|-------------------|------------------------|
| `DOCKER_USERNAME` | ваш_dockerhub_username |
| `DOCKER_PASSWORD` | ваш_dockerhub_password |
| `VPS_HOST`        | IP_адрес_сервера       |
| `VPS_USERNAME`    | root                   |
| `VPS_SSH_KEY`     | приватный SSH ключ     |

## 📦 Деплой

### Автоматический (через GitHub)

```bash
git add .
git commit -m "Deploy"
git push origin main
```

### Ручной (на сервере)

```bash
ssh root@ваш_ip
cd /opt/family-task-manager
bash deploy.sh
```

## 🔍 Мониторинг на сервере

### Статус контейнеров

```bash
ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"
```

### Логи

```bash
# Все логи
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs -f"

# Только бот
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs -f family-task-manager"

# Только БД
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs -f postgres"

# Последние 50 строк
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs --tail=50"
```

### Использование ресурсов

```bash
ssh root@ваш_ip "docker stats"
```

## 🔧 Управление на сервере

### Перезапуск

```bash
ssh root@ваш_ip "docker compose -C /opt/family-task-manager restart"
```

### Остановка

```bash
ssh root@ваш_ip "docker compose -C /opt/family-task-manager down"
```

### Запуск

```bash
ssh root@ваш_ip "docker compose -C /opt/family-task-manager up -d"
```

### Обновление образа

```bash
ssh root@ваш_ip "cd /opt/family-task-manager && docker compose pull && docker compose up -d"
```

## 🗄️ База данных

### Подключение к БД

```bash
ssh root@ваш_ip "docker exec -it family-task-postgres psql -U familytask -d FamilyTaskManager"
```

### Бэкап БД

```bash
ssh root@ваш_ip "docker exec family-task-postgres pg_dump -U familytask FamilyTaskManager > /tmp/backup_\$(date +%Y%m%d_%H%M%S).sql"
scp root@ваш_ip:/tmp/backup_*.sql ./backups/
```

### Восстановление БД

```bash
scp ./backups/backup.sql root@ваш_ip:/tmp/
ssh root@ваш_ip "docker exec -i family-task-postgres psql -U familytask FamilyTaskManager < /tmp/backup.sql"
```

### Проверка таблиц

```bash
ssh root@ваш_ip "docker exec family-task-postgres psql -U familytask -d FamilyTaskManager -c '\dt'"
```

## 🧹 Очистка

### Удаление старых образов

```bash
ssh root@ваш_ip "docker image prune -f"
```

### Полная очистка Docker

```bash
ssh root@ваш_ip "docker system prune -a"
```

### Очистка логов

```bash
ssh root@ваш_ip "truncate -s 0 /var/lib/docker/containers/*/*-json.log"
```

## 🐛 Troubleshooting

### Бот не отвечает

```bash
# 1. Проверьте статус
ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"

# 2. Проверьте логи
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs --tail=100 family-task-manager"

# 3. Перезапустите
ssh root@ваш_ip "docker compose -C /opt/family-task-manager restart family-task-manager"
```

### БД не работает

```bash
# 1. Проверьте статус
ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps postgres"

# 2. Проверьте логи
ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs postgres"

# 3. Проверьте healthcheck
ssh root@ваш_ip "docker inspect family-task-postgres | grep -A 5 Health"
```

### GitHub Actions не работает

```bash
# 1. Проверьте workflow на GitHub
# Actions → Latest run → Logs

# 2. Проверьте SSH подключение
ssh -i ~/.ssh/github-actions root@ваш_ip

# 3. Проверьте Docker Hub
# Убедитесь, что образ загружен: https://hub.docker.com/
```

### Нехватка места на диске

```bash
# Проверка места
ssh root@ваш_ip "df -h"

# Очистка Docker
ssh root@ваш_ip "docker system prune -a --volumes"

# Очистка логов
ssh root@ваш_ip "journalctl --vacuum-time=7d"
```

## 📊 Полезные алиасы

Добавьте в `~/.bashrc` или `~/.zshrc`:

```bash
# Локальные
alias ftm-build='docker build -t family-task-manager:test .'
alias ftm-up='docker compose -f docker-compose.prod.yml up -d'
alias ftm-down='docker compose -f docker-compose.prod.yml down'
alias ftm-logs='docker compose -f docker-compose.prod.yml logs -f'

# Удалённые (замените IP)
alias ftm-ssh='ssh root@ваш_ip'
alias ftm-status='ssh root@ваш_ip "docker compose -C /opt/family-task-manager ps"'
alias ftm-remote-logs='ssh root@ваш_ip "docker compose -C /opt/family-task-manager logs -f"'
alias ftm-deploy='ssh root@ваш_ip "cd /opt/family-task-manager && bash deploy.sh"'
```

## 🔗 Полезные ссылки

- **Быстрый старт**: [QUICK_START.md](../QUICK_START.md)
- **Локальное тестирование**: [LOCAL_TESTING.md](../LOCAL_TESTING.md)
- **Полная инструкция**: [DEPLOYMENT.md](../DEPLOYMENT.md)
- **Docker Hub**: https://hub.docker.com/
- **GitHub Actions**: https://github.com/ваш_username/ваш_repo/actions
- **Timeweb Cloud**: https://timeweb.cloud/
