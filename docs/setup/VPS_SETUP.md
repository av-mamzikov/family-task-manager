# 🖥️ Настройка VPS для деплоя

Единый скрипт для полной настройки VPS за 5 минут.

## Требования

- VPS с Ubuntu 20.04+ (минимум 1GB RAM, 1 CPU)
- SSH доступ с правами root
- Открытые порты: 22 (SSH), 80 (HTTP), 443 (HTTPS), 5000 (Registry)

## Подготовка (на вашем компьютере)

### 1. Сгенерируйте SSH ключи

#### Ключ для вашего доступа (администратор)

```powershell
# Windows
ssh-keygen -t ed25519 -f $HOME\.ssh\deploy_key -C "admin"

# Linux/macOS
ssh-keygen -t ed25519 -f ~/.ssh/deploy_key -C "admin"
```

#### Ключ для GitHub Actions

```powershell
# Windows
ssh-keygen -t ed25519 -f $HOME\.ssh\github_actions_key -C "github-actions"

# Linux/macOS
ssh-keygen -t ed25519 -f ~/.ssh/github_actions_key -C "github-actions"
```

### 2. Скопируйте публичные ключи

```powershell
# Windows - скопируйте содержимое файлов
Get-Content $HOME\.ssh\deploy_key.pub
Get-Content $HOME\.ssh\github_actions_key.pub

# Linux/macOS
cat ~/.ssh/deploy_key.pub
cat ~/.ssh/github_actions_key.pub
```

Сохраните эти ключи - они понадобятся при запуске скрипта.

## Настройка VPS (один скрипт)

### 1. Скачайте и запустите скрипт инициализации

```bash
# Скачайте скрипт
curl -o init-vps.sh https://raw.githubusercontent.com/ваш_username/family-task-manager/main/scripts/init-vps.sh

# Или скопируйте с локальной машины
scp scripts/init-vps.sh root@ваш_ip:/root/

# Запустите скрипт
bash init-vps.sh
```

### 2. Запустить скрипт

```bash
# Подключитесь к ssh
ssh root@ваш_ip_адрес

# Запустите скрипт
bash init-vps.sh
```

### 3. Следуйте инструкциям скрипта

Скрипт запросит:

1. **SSH ключ администратора** - вставьте содержимое `deploy_key.pub`
2. **SSH ключ GitHub Actions** - вставьте содержимое `github_actions_key.pub`
3. **Имя пользователя для Docker Registry** - например, `admin`
4. **Пароль для Docker Registry** - придумайте сильный пароль
5. **Имя пользователя БД** - по умолчанию `familytask`
6. **Пароль для БД** - придумайте сильный пароль
7. **Telegram Bot Token** (опционально) - можно оставить пустым
8. **Telegram Bot Username** (опционально) - можно оставить пустым

### Что установит скрипт:

- ✅ Docker и Docker Compose
- ✅ Базовые утилиты (curl, git, apache2-utils)
- ✅ Пользователь `deploy` с sudo-правами и доступом к Docker
- ✅ SSH ключи для администратора и GitHub Actions
- ✅ Директории `/opt/family-task-manager` и `/opt/docker-registry`
- ✅ Private Docker Registry с аутентификацией
- ✅ Registry UI для просмотра образов
- ✅ Файл `.env` с настройками
- ✅ UFW Firewall (опционально)

## После завершения скрипта

### 1. Проверьте подключение

```bash
# Windows
ssh -i $HOME\.ssh\deploy_key deploy@ваш_ip

# Linux/macOS
ssh -i ~/.ssh/deploy_key deploy@ваш_ip
```

> 🎉 Теперь работайте от пользователя `deploy`, а не от root!

### 2. (Опционально) Настройте SSH config для удобства

Создайте/отредактируйте `~/.ssh/config`:

```
Host myvps
    HostName ваш_ip
    User deploy
    IdentityFile ~/.ssh/deploy_key

Host myvps-root
    HostName ваш_ip
    User root
    IdentityFile ~/.ssh/id_ed25519
```

Теперь можно подключаться просто:
```bash
ssh myvps
```

## Настройка GitHub Secrets

После завершения скрипта вы получите все необходимые данные. Скопируйте их и добавьте в GitHub.

### Получение приватного ключа GitHub Actions

```powershell
# Windows
Get-Content $HOME\.ssh\github_actions_key

# Linux/macOS
cat ~/.ssh/github_actions_key
```

Скопируйте **весь вывод** (включая `-----BEGIN` и `-----END`).

### Добавление секретов в GitHub

Перейдите в репозиторий: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

| Секрет                  | Откуда взять                                   |
|-------------------------|------------------------------------------------|
| `VPS_HOST`              | IP адрес VPS (показан в конце скрипта)         |
| `VPS_USERNAME`          | `deploy`                                       |
| `VPS_SSH_KEY`           | Приватный ключ `github_actions_key` (см. выше) |
| `REGISTRY_USERNAME`     | Имя пользователя registry (вводили в скрипте)  |
| `REGISTRY_PASSWORD`     | Пароль registry (вводили в скрипте)            |
| `POSTGRES_USER`         | Имя пользователя БД (вводили в скрипте)        |
| `POSTGRES_PASSWORD`     | Пароль БД (вводили в скрипте)                  |
| `TELEGRAM_BOT_TOKEN`    | Токен от @BotFather (если вводили в скрипте)   |
| `TELEGRAM_BOT_USERNAME` | Username бота (если вводили в скрипте)         |

> 💡 **Совет:** Скрипт выводит все данные в конце работы - сохраните их!

### Для PR Preview (опционально)

| Секрет                 | Описание                |
|------------------------|-------------------------|
| `PR_BOT_TOKEN`         | Токен тестового бота    |
| `PR_BOT_USERNAME`      | Username тестового бота |
| `PR_POSTGRES_USER`     | `familytask_pr`         |
| `PR_POSTGRES_PASSWORD` | Пароль для PR БД        |

## Первый деплой

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
# Подключитесь к VPS
ssh deploy@ваш_ip

# Проверьте статус контейнеров
cd /opt/family-task-manager
docker compose -f docker-compose.prod.yml ps

# Просмотр логов
docker compose -f docker-compose.prod.yml logs -f
```

### Доступ к Registry UI

После настройки Registry UI доступен по адресу:

```
http://ваш_ip:5001
```

Здесь вы можете просматривать загруженные Docker образы.

## Дополнительно: Установка Portainer (опционально)

Portainer - удобный Web интерфейс для управления Docker контейнерами.

```bash
# Подключитесь к VPS
ssh deploy@ваш_ip

# Создайте директорию
sudo mkdir -p /opt/portainer
sudo chown deploy:deploy /opt/portainer
cd /opt/portainer

# Создайте docker-compose.yml
cat > docker-compose.yml <<'EOF'
version: '3.8'

services:
  portainer:
    image: portainer/portainer-ce:latest
    container_name: portainer
    restart: unless-stopped
    ports:
      - "9000:9000"
      - "9443:9443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - portainer_data:/data

volumes:
  portainer_data:
EOF

# Запустите Portainer
docker compose up -d
```

**Доступ:**

- HTTP: `http://ваш_ip:9000`
- HTTPS: `https://ваш_ip:9443`

При первом входе создайте администратора.

## Troubleshooting

### Registry недоступен

```bash
ssh deploy@ваш_ip

# Проверьте статус registry
cd /opt/docker-registry
docker compose ps

# Проверьте логи
docker compose logs registry

# Перезапустите registry
docker compose restart
```

### Контейнеры приложения не запускаются

```bash
ssh deploy@ваш_ip

# Проверьте логи
cd /opt/family-task-manager
docker compose -f docker-compose.prod.yml logs

# Проверьте .env файл
cat .env

# Проверьте доступность БД
docker compose -f docker-compose.prod.yml exec postgres pg_isready
```

### GitHub Actions не может подключиться к VPS

```bash
ssh deploy@ваш_ip

# Проверьте SSH ключи
cat ~/.ssh/authorized_keys

# Должны быть 2 ключа: ваш и GitHub Actions

# Проверьте права
ls -la ~/.ssh/
# Должно быть:
# drwx------ .ssh
# -rw------- authorized_keys

# Если права неправильные:
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

### Не могу подключиться от пользователя deploy

```bash
# Подключитесь как root
ssh root@ваш_ip

# Проверьте, что пользователь создан
id deploy

# Проверьте SSH ключи
cat /home/deploy/.ssh/authorized_keys

# Если ключей нет, добавьте вручную:
echo "ваш_публичный_ключ" >> /home/deploy/.ssh/authorized_keys
chown deploy:deploy /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
```

### Забыли пароль от Registry

```bash
ssh deploy@ваш_ip
cd /opt/docker-registry/registry-auth

# Создайте нового пользователя
htpasswd -Bc htpasswd новый_пользователь

# Или перезапишите файл
htpasswd -Bc htpasswd admin

# Перезапустите registry
cd /opt/docker-registry
docker compose restart
```

## Полезные команды

### Управление контейнерами

```bash
ssh deploy@ваш_ip

# Статус всех контейнеров
docker ps -a

# Логи приложения
cd /opt/family-task-manager
docker compose -f docker-compose.prod.yml logs -f app

# Логи БД
docker compose -f docker-compose.prod.yml logs -f postgres

# Перезапуск приложения
docker compose -f docker-compose.prod.yml restart app

# Остановка всех контейнеров
docker compose -f docker-compose.prod.yml down

# Запуск всех контейнеров
docker compose -f docker-compose.prod.yml up -d
```

### Управление Registry

```bash
# Просмотр образов в registry
curl -u username:password http://localhost:5000/v2/_catalog

# Удаление образа (через Registry UI)
# Откройте http://ваш_ip:5001

# Очистка неиспользуемых образов
docker system prune -a
```

### Резервное копирование БД

```bash
# Создание бэкапа
docker compose -f docker-compose.prod.yml exec postgres \
  pg_dump -U familytask familytask > backup_$(date +%Y%m%d).sql

# Восстановление из бэкапа
cat backup_20241124.sql | docker compose -f docker-compose.prod.yml exec -T postgres \
  psql -U familytask familytask
```

## Следующие шаги

- 📖 [GitHub Actions Setup](GITHUB_ACTIONS_SETUP.md) - настройка CI/CD
- 🚀 [Deployment Summary](../../DEPLOYMENT_SUMMARY.md) - обзор процесса деплоя
- 🐳 [Docker Registry Setup](../PRIVATE_REGISTRY_SETUP.md) - подробнее о registry

---

**Готово!** Ваш VPS настроен и готов к автоматическому деплою. 🎉
