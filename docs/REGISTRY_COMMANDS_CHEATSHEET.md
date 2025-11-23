# 🚀 Private Registry - Шпаргалка команд

Быстрый справочник по командам для работы с Private Docker Registry.

## 📋 Содержание

- [Первоначальная настройка](#первоначальная-настройка)
- [Ежедневная работа](#ежедневная-работа)
- [Управление Registry](#управление-registry)
- [Управление приложением](#управление-приложением)
- [Диагностика](#диагностика)
- [Обслуживание](#обслуживание)

---

## Первоначальная настройка

### На VPS

```bash
# 1. Создать директорию для registry
sudo mkdir -p /opt/docker-registry && sudo chown $USER:$USER /opt/docker-registry

# 2. Скопировать файлы (выполнить на локальной машине)
scp docker-compose.registry.yml user@vps:/opt/docker-registry/
scp scripts/setup-registry.sh user@vps:/opt/docker-registry/

# 3. Запустить настройку
cd /opt/docker-registry
bash setup-registry.sh

# 4. Проверить статус
docker compose -f docker-compose.registry.yml ps
curl http://localhost:5000/v2/_catalog
```

### На локальной машине

```bash
# Windows (PowerShell)
ssh -L 5000:localhost:5000 -N user@vps-ip

# Linux/Mac
ssh -L 5000:localhost:5000 -N user@vps-ip &

# Войти в registry
docker login localhost:5000
```

---

## Ежедневная работа

### Сборка и деплой (полный цикл)

```bash
# 1. Создать SSH туннель (если еще не создан)
ssh -L 5000:localhost:5000 -N user@vps-ip &

# 2. Собрать и отправить образ
# Windows:
.\scripts\build-and-push.ps1

# Linux/Mac:
bash scripts/build-and-push.sh

# 3. Деплой на VPS
ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
```

### Быстрое обновление

```bash
# Одной командой (если туннель уже создан)
.\scripts\build-and-push.ps1 && ssh user@vps 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
```

---

## Управление Registry

### Просмотр образов

```bash
# Список репозиториев
curl http://localhost:5000/v2/_catalog

# Список тегов для образа
curl http://localhost:5000/v2/family-task-manager/tags/list

# Через UI (в браузере)
http://vps-ip:5001
```

### Управление контейнером registry

```bash
# Статус
docker compose -f /opt/docker-registry/docker-compose.registry.yml ps

# Логи
docker logs docker-registry -f

# Перезапуск
docker compose -f /opt/docker-registry/docker-compose.registry.yml restart

# Остановка
docker compose -f /opt/docker-registry/docker-compose.registry.yml down

# Запуск
docker compose -f /opt/docker-registry/docker-compose.registry.yml up -d
```

### Очистка registry

```bash
# Удалить неиспользуемые слои
docker exec docker-registry bin/registry garbage-collect /etc/docker/registry/config.yml

# Удалить конкретный образ (требует включения delete в config)
# 1. Получить digest
curl -I -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
  http://localhost:5000/v2/family-task-manager/manifests/old-tag

# 2. Удалить
curl -X DELETE http://localhost:5000/v2/family-task-manager/manifests/sha256:...

# 3. Запустить garbage collection
docker exec docker-registry bin/registry garbage-collect /etc/docker/registry/config.yml
```

---

## Управление приложением

### Деплой

```bash
# Полный деплой
cd /opt/family-task-manager
bash scripts/deploy-from-registry.sh

# Только pull нового образа
docker compose -f docker-compose.prod.yml pull

# Перезапуск с новым образом
docker compose -f docker-compose.prod.yml up -d --force-recreate family-task-manager
```

### Статус и логи

```bash
# Статус всех контейнеров
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps

# Логи приложения
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f family-task-manager

# Логи БД
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f postgres

# Последние 100 строк
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs --tail=100 family-task-manager
```

### Управление контейнерами

```bash
# Перезапуск приложения
docker compose -f /opt/family-task-manager/docker-compose.prod.yml restart family-task-manager

# Остановка
docker compose -f /opt/family-task-manager/docker-compose.prod.yml stop family-task-manager

# Запуск
docker compose -f /opt/family-task-manager/docker-compose.prod.yml start family-task-manager

# Полная остановка (включая БД)
docker compose -f /opt/family-task-manager/docker-compose.prod.yml down

# Полный запуск
docker compose -f /opt/family-task-manager/docker-compose.prod.yml up -d
```

### Откат на предыдущую версию

```bash
# 1. Посмотреть доступные теги
docker images localhost:5000/family-task-manager

# 2. Изменить тег в .env или docker-compose
export REGISTRY_TAG=previous-commit-hash

# 3. Pull и перезапуск
docker compose -f /opt/family-task-manager/docker-compose.prod.yml pull
docker compose -f /opt/family-task-manager/docker-compose.prod.yml up -d --force-recreate family-task-manager
```

---

## Диагностика

### Проверка доступности

```bash
# Registry доступен?
curl -f http://localhost:5000/v2/_catalog && echo "✓ OK" || echo "✗ FAIL"

# Приложение работает?
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps | grep "Up"

# БД доступна?
docker exec family-task-postgres pg_isready -U familytask
```

### Использование ресурсов

```bash
# Статистика контейнеров
docker stats --no-stream

# Использование диска
df -h
docker system df

# Размер образов
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}"

# Размер volumes
docker volume ls
du -sh /var/lib/docker/volumes/*
```

### Проверка логов на ошибки

```bash
# Ошибки в приложении
docker logs family-task-manager 2>&1 | grep -i error

# Ошибки в БД
docker logs family-task-postgres 2>&1 | grep -i error

# Ошибки в registry
docker logs docker-registry 2>&1 | grep -i error
```

### Проверка сети

```bash
# Список сетей
docker network ls

# Инспекция сети приложения
docker network inspect family-task-manager_family-task-network

# Проверка связи между контейнерами
docker exec family-task-manager ping -c 3 postgres
```

---

## Обслуживание

### Резервное копирование

```bash
# Бэкап БД
docker exec family-task-postgres pg_dump -U familytask FamilyTaskManager > \
  /opt/backups/family-task-manager/backup_$(date +%Y%m%d_%H%M%S).sql

# Бэкап .env файла
cp /opt/family-task-manager/.env /opt/backups/family-task-manager/.env.backup

# Бэкап registry данных
sudo tar -czf /opt/backups/registry_$(date +%Y%m%d).tar.gz \
  /opt/docker-registry/registry-data

# Список бэкапов
ls -lh /opt/backups/family-task-manager/
```

### Восстановление из бэкапа

```bash
# Остановить приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml down

# Восстановить БД
cat /opt/backups/family-task-manager/backup_20241123_120000.sql | \
  docker exec -i family-task-postgres psql -U familytask FamilyTaskManager

# Запустить приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml up -d
```

### Очистка

```bash
# Удалить неиспользуемые образы
docker image prune -a -f

# Удалить неиспользуемые volumes
docker volume prune -f

# Удалить все неиспользуемое
docker system prune -a --volumes -f

# Очистка старых бэкапов (старше 30 дней)
find /opt/backups/family-task-manager/ -name "*.sql" -mtime +30 -delete
```

### Обновление компонентов

```bash
# Обновить Docker
sudo apt update && sudo apt upgrade docker-ce docker-ce-cli containerd.io

# Обновить Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" \
  -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Обновить образ registry
cd /opt/docker-registry
docker compose -f docker-compose.registry.yml pull
docker compose -f docker-compose.registry.yml up -d
```

---

## Полезные алиасы

Добавьте в `~/.bashrc` или `~/.zshrc`:

```bash
# Алиасы для приложения
alias app-logs='docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f family-task-manager'
alias app-status='docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps'
alias app-restart='docker compose -f /opt/family-task-manager/docker-compose.prod.yml restart family-task-manager'
alias app-deploy='cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'

# Алиасы для registry
alias reg-status='docker compose -f /opt/docker-registry/docker-compose.registry.yml ps'
alias reg-logs='docker logs docker-registry -f'
alias reg-list='curl -s http://localhost:5000/v2/_catalog | jq'

# Алиасы для БД
alias db-backup='docker exec family-task-postgres pg_dump -U familytask FamilyTaskManager > /opt/backups/family-task-manager/backup_$(date +%Y%m%d_%H%M%S).sql'
alias db-size='docker exec family-task-postgres psql -U familytask -d FamilyTaskManager -c "SELECT pg_size_pretty(pg_database_size(current_database()));"'

# Алиасы для мониторинга
alias docker-stats='docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"'
alias disk-usage='df -h && echo && docker system df'
```

Применить:

```bash
source ~/.bashrc  # или ~/.zshrc
```

---

## SSH Config для удобства

Добавьте в `~/.ssh/config`:

```
Host vps
    HostName your-vps-ip
    User your-user
    IdentityFile ~/.ssh/id_rsa

Host vps-registry
    HostName your-vps-ip
    User your-user
    IdentityFile ~/.ssh/id_rsa
    LocalForward 5000 localhost:5000
    LocalForward 5001 localhost:5001
```

Использование:

```bash
# Обычное подключение
ssh vps

# С туннелями для registry
ssh vps-registry
```

---

## Быстрые проверки

### Healthcheck скрипт

Создайте `/opt/scripts/healthcheck.sh`:

```bash
#!/bin/bash

echo "=== Health Check ==="
echo ""

# Registry
echo "Registry:"
curl -sf http://localhost:5000/v2/_catalog > /dev/null && echo "  ✓ OK" || echo "  ✗ FAIL"

# Application
echo "Application:"
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps | grep -q "family-task-manager.*Up" && echo "  ✓ OK" || echo "  ✗ FAIL"

# Database
echo "Database:"
docker exec family-task-postgres pg_isready -U familytask > /dev/null 2>&1 && echo "  ✓ OK" || echo "  ✗ FAIL"

# Disk space
echo ""
echo "Disk usage:"
df -h / | tail -1 | awk '{print "  Used: "$3" / "$2" ("$5")"}'

# Memory
echo ""
echo "Memory usage:"
free -h | grep Mem | awk '{print "  Used: "$3" / "$2}'

echo ""
echo "=== End ==="
```

Запуск:

```bash
bash /opt/scripts/healthcheck.sh
```

---

## Troubleshooting команды

```bash
# Registry не отвечает
docker logs docker-registry --tail=50
docker compose -f /opt/docker-registry/docker-compose.registry.yml restart

# Приложение не запускается
docker logs family-task-manager --tail=100
docker compose -f /opt/family-task-manager/docker-compose.prod.yml up -d --force-recreate

# БД не подключается
docker logs family-task-postgres --tail=50
docker exec family-task-postgres psql -U familytask -l

# Нет места на диске
docker system prune -a --volumes -f
find /opt/backups -mtime +30 -delete

# SSH туннель не работает
ps aux | grep "ssh.*5000"
pkill -f "ssh.*5000"
ssh -L 5000:localhost:5000 -N user@vps &

# Не могу push в registry
docker login localhost:5000
curl http://localhost:5000/v2/_catalog
```

---

## 📚 Дополнительные ресурсы

- [Полная документация](PRIVATE_REGISTRY_SETUP.md)
- [Архитектура](REGISTRY_ARCHITECTURE.md)
- [Чек-лист деплоя](../.github/DEPLOYMENT_CHECKLIST.md)
- [Сравнение вариантов](DEPLOYMENT_OPTIONS.md)
