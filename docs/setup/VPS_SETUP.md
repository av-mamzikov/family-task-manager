# 🖥️ Настройка VPS для деплоя

Пошаговая инструкция по настройке VPS для развертывания Family Task Manager.

## Требования

- VPS с Ubuntu 20.04+ (минимум 1GB RAM, 1 CPU)
- SSH доступ с правами root
- Открытые порты: 22 (SSH), 80 (HTTP), 443 (HTTPS), 5000 (Registry)

## Шаг 1: Подключитесь к VPS

```bash
ssh root@ваш_ip_адрес
```

## Шаг 2: Автоматическая настройка сервера

Скопируйте и запустите скрипт настройки:

```bash
# На вашем компьютере
scp scripts/server-setup.sh root@ваш_ip:/tmp/

# На VPS
ssh root@ваш_ip
bash /tmp/server-setup.sh
```

**Что установит скрипт:**

- ✅ Docker и Docker Compose
- ✅ Базовые утилиты (curl, git, etc.)
- ✅ Настройка firewall (ufw)
- ✅ Создание пользователя для деплоя

## Шаг 3: Настройка Private Registry

```bash
# Скопируйте файлы на VPS (на вашем компьютере)
scp docker-compose.registry.yml root@ваш_ip:/tmp/
scp scripts/setup-registry.sh root@ваш_ip:/tmp/

# На VPS
ssh root@ваш_ip
mkdir -p /opt/docker-registry
cd /opt/docker-registry
mv /tmp/docker-compose.registry.yml ./
mv /tmp/setup-registry.sh ./
bash setup-registry.sh
```

**Важно:** Запомните username и пароль для registry - они понадобятся для GitHub Secrets!

**Что настроит скрипт:**

- ✅ Docker Registry контейнер
- ✅ Базовая аутентификация
- ✅ Persistent storage для образов
- ✅ Автозапуск при перезагрузке

## Шаг 4: Создайте SSH ключ для GitHub Actions

### Windows

```powershell
# Генерация ключа
ssh-keygen -t ed25519 -f $HOME\.ssh\github_actions_key -C "github-actions"

# Скопируйте публичный ключ на VPS
Get-Content $HOME\.ssh\github_actions_key.pub | ssh root@ваш_ip "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"

# Скопируйте приватный ключ (понадобится для GitHub Secrets)
Get-Content $HOME\.ssh\github_actions_key
```

### Linux/macOS

```bash
# Генерация ключа
ssh-keygen -t ed25519 -f ~/.ssh/github_actions_key -C "github-actions"

# Скопируйте публичный ключ на VPS
ssh-copy-id -i ~/.ssh/github_actions_key.pub root@ваш_ip

# Скопируйте приватный ключ (понадобится для GitHub Secrets)
cat ~/.ssh/github_actions_key
```

## Шаг 5: Настройте GitHub Secrets

Перейдите в репозиторий: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

### Обязательные секреты

| Секрет                  | Описание              | Пример         |
|-------------------------|-----------------------|----------------|
| `VPS_HOST`              | IP адрес VPS          | `123.45.67.89` |
| `VPS_USERNAME`          | SSH username          | `root`         |
| `VPS_SSH_KEY`           | Приватный SSH ключ    | Из шага 4      |
| `REGISTRY_USERNAME`     | Username registry     | Из шага 3      |
| `REGISTRY_PASSWORD`     | Пароль registry       | Из шага 3      |
| `TELEGRAM_BOT_TOKEN`    | Токен production бота | От @BotFather  |
| `TELEGRAM_BOT_USERNAME` | Username бота         | `your_bot`     |
| `POSTGRES_USER`         | PostgreSQL user       | `familytask`   |
| `POSTGRES_PASSWORD`     | PostgreSQL пароль     | Сильный пароль |

### Для PR Preview (опционально)

| Секрет                 | Описание                |
|------------------------|-------------------------|
| `PR_BOT_TOKEN`         | Токен тестового бота    |
| `PR_BOT_USERNAME`      | Username тестового бота |
| `PR_POSTGRES_USER`     | `familytask_pr`         |
| `PR_POSTGRES_PASSWORD` | Пароль для PR БД        |

## Шаг 6: Первый деплой

Всё готово! Теперь просто запушьте код:

```bash
git add .
git commit -m "Setup deployment"
git push origin main
```

### Что произойдёт автоматически

1. ✅ Запустятся тесты
2. ✅ Соберётся Docker образ
3. ✅ Образ загрузится в registry на VPS
4. ✅ `docker-compose.prod.yml` скопируется на VPS
5. ✅ Приложение задеплоится и запустится
6. ✅ EF Core создаст схему БД автоматически

### Проверка деплоя

```bash
# На VPS проверьте статус
ssh root@ваш_ip
cd /opt/family-task-manager
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f
```

## Шаг 7 (опционально): Установите Portainer

Portainer - удобный Web интерфейс для управления Docker контейнерами.

```bash
# Скопируйте конфиг (на вашем компьютере)
scp docker-compose.portainer.yml root@ваш_ip:/opt/portainer/docker-compose.yml

# На VPS запустите Portainer
ssh root@ваш_ip
mkdir -p /opt/portainer
cd /opt/portainer
docker compose up -d
```

**Доступ:** `http://ваш_ip:9000` или `https://ваш_ip:9443`

**Подробнее:** [Portainer Setup](../PORTAINER_SETUP.md)

## Troubleshooting

### Registry недоступен

```bash
# Проверьте статус registry
docker ps | grep registry

# Проверьте логи
docker logs registry

# Перезапустите registry
cd /opt/docker-registry
docker compose restart
```

### Контейнеры не запускаются

```bash
# Проверьте логи
docker compose -f docker-compose.prod.yml logs

# Проверьте .env файл
cat /opt/family-task-manager/.env

# Проверьте доступность БД
docker exec family-task-postgres pg_isready
```

### GitHub Actions не может подключиться

```bash
# Проверьте SSH ключи на VPS
cat ~/.ssh/authorized_keys

# Проверьте права
chmod 600 ~/.ssh/authorized_keys
chmod 700 ~/.ssh
```

## Дополнительная настройка

### Firewall (ufw)

```bash
# Проверьте статус
sudo ufw status

# Разрешите нужные порты
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS
sudo ufw enable
```

### Автоматические обновления

```bash
# Установите unattended-upgrades
sudo apt install unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades
```

### Мониторинг

Рекомендуется установить:

- **Portainer** - для управления контейнерами
- **Prometheus + Grafana** - для метрик
- **Loki** - для централизованных логов

## Следующие шаги

- 📖 [GitHub Actions Setup](GITHUB_ACTIONS_SETUP.md)
- 🐳 [Portainer Setup](../PORTAINER_SETUP.md)
- 🔒 [Private Registry Setup](../PRIVATE_REGISTRY_SETUP.md)
- 🚀 [Deployment Summary](../../DEPLOYMENT_SUMMARY.md)
