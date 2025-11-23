# Инструкция по развёртыванию Family Task Manager

Полное руководство по настройке автоматического деплоя с GitHub на VPS.

## Содержание

- [Требования](#требования)
- [Шаг 1: Настройка Docker Hub](#шаг-1-настройка-docker-hub)
- [Шаг 2: Настройка VPS сервера](#шаг-2-настройка-vps-сервера)
- [Шаг 3: Настройка GitHub Secrets](#шаг-3-настройка-github-secrets)
- [Шаг 4: Первый деплой](#шаг-4-первый-деплой)
- [Управление приложением](#управление-приложением)
- [Мониторинг и логи](#мониторинг-и-логи)
- [Troubleshooting](#troubleshooting)

## Требования

- VPS сервер (рекомендуется: Timeweb Cloud, 1GB RAM минимум)
- Ubuntu 22.04 или новее
- Доступ по SSH
- Аккаунт на Docker Hub (бесплатный)
- GitHub репозиторий

## Шаг 1: Настройка Docker Hub

1. Зарегистрируйтесь на [Docker Hub](https://hub.docker.com/)
2. Создайте репозиторий `family-task-manager`
3. Сохраните ваш username и пароль для следующих шагов

## Шаг 2: Настройка VPS сервера

### 2.1 Подключение к серверу

```bash
ssh root@ваш_ip_адрес
```

### 2.2 Автоматическая настройка

Скопируйте скрипт `scripts/server-setup.sh` на сервер и запустите:

```bash
# На вашем компьютере
scp scripts/server-setup.sh root@ваш_ip:/tmp/

# На сервере
ssh root@ваш_ip
sudo bash /tmp/server-setup.sh
```

### 2.3 Ручная настройка (альтернатива)

Если предпочитаете настроить вручную:

```bash
# Обновление системы
apt-get update && apt-get upgrade -y

# Установка Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Установка Docker Compose
DOCKER_COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep 'tag_name' | cut -d\" -f4)
curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Создание директории проекта
mkdir -p /opt/family-task-manager
cd /opt/family-task-manager
```

### 2.4 Настройка переменных окружения

Создайте файл `/opt/family-task-manager/.env`:

```bash
nano /opt/family-task-manager/.env
```

Содержимое:

```env
# Docker Hub
DOCKER_USERNAME=ваш_dockerhub_username

# PostgreSQL
POSTGRES_USER=familytask
POSTGRES_PASSWORD=ваш_сильный_пароль_для_БД

# Telegram Bot
TELEGRAM_BOT_TOKEN=ваш_токен_бота
TELEGRAM_BOT_USERNAME=ваш_username_бота
```

**Важно**: Используйте сильные пароли!

### 2.5 Копирование файлов

Скопируйте необходимые файлы на сервер:

```bash
# На вашем компьютере
scp docker-compose.prod.yml root@ваш_ip:/opt/family-task-manager/docker-compose.yml
scp scripts/init-db.sql root@ваш_ip:/opt/family-task-manager/scripts/
scp scripts/deploy.sh root@ваш_ip:/opt/family-task-manager/
```

Сделайте скрипт деплоя исполняемым:

```bash
# На сервере
chmod +x /opt/family-task-manager/deploy.sh
```

### 2.6 Настройка SSH ключа для GitHub Actions

Создайте SSH ключ для деплоя:

```bash
# На сервере
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github-actions -N ""

# Добавьте публичный ключ в authorized_keys
cat ~/.ssh/github-actions.pub >> ~/.ssh/authorized_keys

# Выведите приватный ключ (сохраните его для GitHub Secrets)
cat ~/.ssh/github-actions
```

**Важно**: Сохраните приватный ключ, он понадобится для настройки GitHub Secrets.

## Шаг 3: Настройка GitHub Secrets

1. Откройте ваш репозиторий на GitHub
2. Перейдите в `Settings` → `Secrets and variables` → `Actions`
3. Нажмите `New repository secret` и добавьте следующие секреты:

| Имя секрета       | Значение               | Описание                   |
|-------------------|------------------------|----------------------------|
| `DOCKER_USERNAME` | ваш_dockerhub_username | Username от Docker Hub     |
| `DOCKER_PASSWORD` | ваш_dockerhub_password | Пароль от Docker Hub       |
| `VPS_HOST`        | IP_адрес_сервера       | IP адрес вашего VPS        |
| `VPS_USERNAME`    | root                   | Имя пользователя для SSH   |
| `VPS_SSH_KEY`     | приватный_ssh_ключ     | Приватный ключ из шага 2.6 |

## Шаг 4: Первый деплой

### 4.1 Автоматический деплой через GitHub

1. Закоммитьте и запушьте изменения в ветку `main` или `master`:

```bash
git add .
git commit -m "Setup deployment"
git push origin main
```

2. GitHub Actions автоматически:
    - Соберёт Docker образ
    - Загрузит его в Docker Hub
    - Задеплоит на ваш VPS

3. Проверьте статус в разделе `Actions` на GitHub

### 4.2 Ручной деплой (если нужно)

На сервере выполните:

```bash
cd /opt/family-task-manager
bash deploy.sh
```

## Управление приложением

### Просмотр статуса

```bash
cd /opt/family-task-manager
docker compose ps
```

### Просмотр логов

```bash
# Все логи
docker compose logs -f

# Только бот
docker compose logs -f family-task-manager

# Только БД
docker compose logs -f postgres
```

### Перезапуск

```bash
cd /opt/family-task-manager
docker compose restart
```

### Остановка

```bash
cd /opt/family-task-manager
docker compose down
```

### Запуск

```bash
cd /opt/family-task-manager
docker compose up -d
```

## Мониторинг и логи

### Проверка использования ресурсов

```bash
docker stats
```

### Очистка старых образов

```bash
docker system prune -a
```

### Бэкап базы данных

```bash
docker exec family-task-postgres pg_dump -U familytask FamilyTaskManager > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Восстановление из бэкапа

```bash
docker exec -i family-task-postgres psql -U familytask FamilyTaskManager < backup.sql
```

## Troubleshooting

### Проблема: Контейнер не запускается

**Решение**: Проверьте логи

```bash
docker compose logs family-task-manager
```

### Проблема: Ошибка подключения к БД

**Решение**: Проверьте, что PostgreSQL запущен и здоров

```bash
docker compose ps postgres
docker compose logs postgres
```

### Проблема: GitHub Actions не может подключиться к серверу

**Решение**:

1. Проверьте правильность `VPS_SSH_KEY` в GitHub Secrets
2. Убедитесь, что публичный ключ добавлен в `~/.ssh/authorized_keys` на сервере
3. Проверьте, что SSH порт открыт (обычно 22)

### Проблема: Бот не отвечает

**Решение**:

1. Проверьте, что `TELEGRAM_BOT_TOKEN` правильный
2. Проверьте логи бота: `docker compose logs -f family-task-manager`
3. Убедитесь, что контейнер запущен: `docker compose ps`

### Проблема: Недостаточно памяти

**Решение**:

1. Увеличьте размер VPS
2. Или настройте swap:

```bash
fallocate -l 2G /swapfile
chmod 600 /swapfile
mkswap /swapfile
swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
```

## Обновление приложения

### Автоматическое обновление

Просто запушьте изменения в `main` ветку - GitHub Actions автоматически задеплоит новую версию.

### Ручное обновление

```bash
cd /opt/family-task-manager
bash deploy.sh
```

## Безопасность

### Рекомендации:

1. **Используйте сильные пароли** для БД
2. **Настройте firewall**:
   ```bash
   ufw allow 22/tcp
   ufw enable
   ```
3. **Регулярно обновляйте систему**:
   ```bash
   apt-get update && apt-get upgrade -y
   ```
4. **Настройте автоматические бэкапы БД** (cron job)
5. **Не храните секреты в коде** - используйте переменные окружения

## Стоимость

При использовании **Timeweb Cloud** (тариф Cloud 1):

- Стоимость: **169₽/месяц**
- Ресурсы: 1 vCPU, 1GB RAM, 10GB SSD
- Достаточно для работы бота с небольшой нагрузкой

## Поддержка

Если возникли проблемы:

1. Проверьте логи: `docker compose logs -f`
2. Проверьте статус: `docker compose ps`
3. Проверьте переменные окружения в `.env`
4. Убедитесь, что все GitHub Secrets настроены правильно

---

**Готово!** Ваш бот теперь автоматически деплоится при каждом пуше в `main` ветку.
