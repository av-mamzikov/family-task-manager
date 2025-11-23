# Настройка Private Docker Registry на VPS

Полное руководство по развертыванию приложения через собственный Docker Registry.

## Архитектура

```
┌─────────────────┐                    ┌──────────────────────────────┐
│  Локальная      │   SSH Tunnel       │         VPS Server           │
│  машина         │◄──────────────────►│                              │
│                 │   localhost:5000   │  ┌────────────────────────┐  │
│  1. Build       │                    │  │  Private Registry      │  │
│  2. Push ───────┼───────────────────►│  │  :5000                 │  │
│                 │                    │  └──────────┬─────────────┘  │
└─────────────────┘                    │             │                │
                                       │             │ pull           │
                                       │             ▼                │
                                       │  ┌────────────────────────┐  │
                                       │  │  Docker Compose        │  │
                                       │  │  - App Container       │  │
                                       │  │  - PostgreSQL          │  │
                                       │  └────────────────────────┘  │
                                       └──────────────────────────────┘
```

## Преимущества этого подхода

✅ **Полный контроль** - все данные на вашем VPS  
✅ **Безопасность** - образы не покидают вашу инфраструктуру  
✅ **Быстрый деплой** - pull быстрее, чем build на VPS  
✅ **Версионирование** - храните несколько версий образов  
✅ **CI/CD ready** - легко интегрируется с GitHub Actions

---

## Шаг 1: Настройка Registry на VPS

### 1.1. Подключитесь к VPS

```bash
ssh user@your-vps-ip
```

### 1.2. Создайте директорию для registry

```bash
sudo mkdir -p /opt/docker-registry
sudo chown $USER:$USER /opt/docker-registry
cd /opt/docker-registry
```

### 1.3. Скопируйте конфигурационные файлы

С локальной машины:

```bash
scp docker-compose.registry.yml user@vps-ip:/opt/docker-registry/
scp scripts/setup-registry.sh user@vps-ip:/opt/docker-registry/
```

### 1.4. Запустите скрипт настройки

На VPS:

```bash
cd /opt/docker-registry
bash setup-registry.sh
```

Скрипт выполнит:

- Установку необходимых пакетов
- Создание пользователя для аутентификации
- Настройку Docker daemon
- Запуск Registry и UI

### 1.5. Проверьте работу Registry

```bash
# Проверка статуса
docker compose -f docker-compose.registry.yml ps

# Проверка API
curl http://localhost:5000/v2/_catalog

# Должен вернуть: {"repositories":[]}
```

**Registry UI** будет доступен по адресу: `http://your-vps-ip:5001`

---

## Шаг 2: Настройка локальной машины

### 2.1. Создайте SSH туннель к Registry

Для безопасной работы с registry используйте SSH туннель:

```bash
# Linux/Mac
ssh -L 5000:localhost:5000 -N user@vps-ip

# Windows (PowerShell)
ssh -L 5000:localhost:5000 -N user@vps-ip
```

Оставьте это окно терминала открытым. Теперь `localhost:5000` на вашей машине указывает на registry на VPS.

> **Совет:** Добавьте алиас в `~/.ssh/config`:
> ```
> Host vps-registry
>   HostName your-vps-ip
>   User your-user
>   LocalForward 5000 localhost:5000
> ```
> Тогда можно просто: `ssh vps-registry`

### 2.2. Войдите в Registry

```bash
docker login localhost:5000
# Введите username и password, созданные на шаге 1.4
```

Credentials сохранятся в `~/.docker/config.json`.

---

## Шаг 3: Сборка и отправка образа

### Вариант A: Локальная сборка (Windows)

```powershell
# Убедитесь, что SSH туннель активен
.\scripts\build-and-push.ps1
```

### Вариант B: Локальная сборка (Linux/Mac)

```bash
# Убедитесь, что SSH туннель активен
bash scripts/build-and-push.sh
```

### Вариант C: С переменными окружения

```bash
# Если registry на другом хосте
export REGISTRY_HOST="your-vps-ip:5000"
export REGISTRY_USER="your-username"
export REGISTRY_PASSWORD="your-password"

bash scripts/build-and-push.sh
```

Скрипт выполнит:

1. ✅ Проверку доступности registry
2. 🔨 Сборку Docker образа
3. 🏷️ Создание тегов (latest, commit hash, branch)
4. 📤 Отправку образа в registry

---

## Шаг 4: Деплой на VPS

### 4.1. Подготовьте приложение на VPS

```bash
# Подключитесь к VPS
ssh user@vps-ip

# Создайте директорию для приложения
sudo mkdir -p /opt/family-task-manager
sudo chown $USER:$USER /opt/family-task-manager
cd /opt/family-task-manager

# Клонируйте репозиторий (или скопируйте нужные файлы)
git clone <your-repo-url> .

# Или скопируйте только необходимые файлы:
# - docker-compose.prod.yml
# - scripts/deploy-from-registry.sh
# - scripts/init-db.sql
# - .env (создайте из .env.example)
```

### 4.2. Настройте .env файл

```bash
cd /opt/family-task-manager
cp .env.example .env
nano .env
```

Заполните:

```env
# Registry (необязательно, по умолчанию localhost:5000)
REGISTRY_HOST=localhost:5000

# PostgreSQL
POSTGRES_USER=familytask
POSTGRES_PASSWORD=your-secure-password

# Telegram Bot
TELEGRAM_BOT_TOKEN=your-bot-token
TELEGRAM_BOT_USERNAME=your_bot_username
```

### 4.3. Запустите деплой

```bash
cd /opt/family-task-manager
bash scripts/deploy-from-registry.sh
```

Скрипт выполнит:

1. ✅ Проверку доступности registry
2. 💾 Создание бэкапа БД
3. 📥 Загрузку нового образа
4. 🔄 Перезапуск контейнера
5. ✅ Проверку здоровья приложения

---

## Шаг 5: Автоматизация через GitHub Actions (опционально)

### 5.1. Создайте GitHub Secrets

В настройках репозитория добавьте:

- `VPS_HOST` - IP адрес VPS
- `VPS_USER` - пользователь для SSH
- `VPS_SSH_KEY` - приватный SSH ключ
- `REGISTRY_USER` - пользователь registry
- `REGISTRY_PASSWORD` - пароль registry

### 5.2. Создайте workflow

```yaml
# .github/workflows/deploy-to-registry.yml
name: Build and Deploy to Private Registry

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup SSH tunnel to registry
        run: |
          mkdir -p ~/.ssh
          echo "${{ secrets.VPS_SSH_KEY }}" > ~/.ssh/id_rsa
          chmod 600 ~/.ssh/id_rsa
          ssh-keyscan ${{ secrets.VPS_HOST }} >> ~/.ssh/known_hosts

          # Запуск SSH туннеля в фоне
          ssh -f -N -L 5000:localhost:5000 ${{ secrets.VPS_USER }}@${{ secrets.VPS_HOST }}
          sleep 5

      - name: Login to Private Registry
        run: |
          echo "${{ secrets.REGISTRY_PASSWORD }}" | docker login localhost:5000 -u ${{ secrets.REGISTRY_USER }} --password-stdin

      - name: Build and Push
        run: |
          export REGISTRY_HOST=localhost:5000
          bash scripts/build-and-push.sh

      - name: Deploy on VPS
        run: |
          ssh ${{ secrets.VPS_USER }}@${{ secrets.VPS_HOST }} \
            'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
```

---

## Повседневное использование

### Обновление приложения

```bash
# 1. Локально: соберите и отправьте новый образ
ssh -L 5000:localhost:5000 -N user@vps-ip &  # в фоне
./scripts/build-and-push.ps1

# 2. На VPS: разверните новую версию
ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
```

### Просмотр образов в Registry

```bash
# Через API
curl http://localhost:5000/v2/_catalog
curl http://localhost:5000/v2/family-task-manager/tags/list

# Через UI
# Откройте в браузере: http://your-vps-ip:5001
```

### Откат на предыдущую версию

```bash
# На VPS
cd /opt/family-task-manager

# Посмотрите доступные теги
docker images localhost:5000/family-task-manager

# Измените тег в docker-compose.prod.yml или .env
export REGISTRY_TAG=abc123  # commit hash

# Перезапустите
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d family-task-manager
```

### Очистка старых образов

```bash
# На VPS
docker image prune -a -f

# Очистка в registry (освобождение места)
docker exec docker-registry bin/registry garbage-collect /etc/docker/registry/config.yml
```

---

## Мониторинг и обслуживание

### Проверка статуса

```bash
# Registry
docker compose -f /opt/docker-registry/docker-compose.registry.yml ps

# Приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps
```

### Логи

```bash
# Registry
docker logs docker-registry -f

# Приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f
```

### Резервное копирование

```bash
# Бэкап registry данных
sudo tar -czf registry-backup-$(date +%Y%m%d).tar.gz /opt/docker-registry/registry-data

# Бэкап БД (автоматически создается при деплое)
ls -lh /opt/backups/family-task-manager/
```

---

## Безопасность

### Рекомендации

1. **Используйте HTTPS** - настройте Nginx с SSL перед registry
2. **Firewall** - закройте порт 5000 извне, оставьте только SSH
3. **Сильные пароли** - для registry и PostgreSQL
4. **Регулярные обновления** - обновляйте образ registry
5. **Мониторинг** - следите за использованием диска

### Настройка Nginx с SSL (опционально)

```nginx
# /etc/nginx/sites-available/registry
server {
    listen 443 ssl http2;
    server_name registry.yourdomain.com;
    
    ssl_certificate /etc/letsencrypt/live/registry.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/registry.yourdomain.com/privkey.pem;
    
    client_max_body_size 0;
    chunked_transfer_encoding on;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## Troubleshooting

### Registry недоступен

```bash
# Проверьте, запущен ли registry
docker ps | grep registry

# Проверьте логи
docker logs docker-registry

# Перезапустите
cd /opt/docker-registry
docker compose -f docker-compose.registry.yml restart
```

### Ошибка "connection refused" при push

```bash
# Проверьте SSH туннель
ps aux | grep "ssh.*5000"

# Пересоздайте туннель
ssh -L 5000:localhost:5000 -N user@vps-ip
```

### Недостаточно места на диске

```bash
# Проверьте использование
df -h
docker system df

# Очистите
docker system prune -a --volumes
```

### Образ не обновляется

```bash
# Принудительно загрузите новый образ
docker pull localhost:5000/family-task-manager:latest --no-cache

# Пересоздайте контейнер
docker compose -f docker-compose.prod.yml up -d --force-recreate family-task-manager
```

---

## Сравнение с другими вариантами

| Критерий                        | Private Registry | Build на VPS        | Docker Hub         | GHCR          |
|---------------------------------|------------------|---------------------|--------------------|---------------|
| Приватность                     | ✅ Полная         | ✅ Полная            | ❌ Требует подписку | ✅ Да          |
| Скорость деплоя                 | ✅ Быстро (pull)  | ⚠️ Медленно (build) | ✅ Быстро           | ✅ Быстро      |
| Ресурсы VPS                     | ✅ Минимальные    | ❌ Много RAM/CPU     | ✅ Минимальные      | ✅ Минимальные |
| Сложность настройки             | ⚠️ Средняя       | ✅ Простая           | ✅ Простая          | ✅ Простая     |
| Зависимость от внешних сервисов | ✅ Нет            | ✅ Нет               | ❌ Да               | ❌ Да          |
| Версионирование                 | ✅ Да             | ⚠️ Ограниченное     | ✅ Да               | ✅ Да          |

---

## Заключение

Вы настроили полноценный CI/CD pipeline с Private Docker Registry:

1. ✅ Registry работает на вашем VPS
2. ✅ Образы собираются локально или на CI
3. ✅ Деплой выполняется одной командой
4. ✅ Все данные под вашим контролем

**Следующие шаги:**

- Настройте автоматический деплой через GitHub Actions
- Добавьте мониторинг (Prometheus + Grafana)
- Настройте автоматические бэкапы
