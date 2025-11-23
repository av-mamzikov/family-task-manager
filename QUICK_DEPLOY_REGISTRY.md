# 🚀 Быстрый старт: Деплой через Private Registry

Краткая инструкция для развертывания приложения через собственный Docker Registry на VPS.

## 📋 Предварительные требования

- VPS с Docker и Docker Compose
- SSH доступ к VPS
- Git репозиторий с кодом

---

## ⚡ Быстрая настройка (5 шагов)

### 1️⃣ Настройте Registry на VPS

```bash
# На VPS
ssh user@vps-ip

# Создайте директорию
sudo mkdir -p /opt/docker-registry && sudo chown $USER:$USER /opt/docker-registry

# Скопируйте файлы (с локальной машины)
scp docker-compose.registry.yml scripts/setup-registry.sh user@vps-ip:/opt/docker-registry/

# Запустите настройку (на VPS)
cd /opt/docker-registry
bash setup-registry.sh
```

**Результат:** Registry работает на `localhost:5000`, UI доступен на `http://vps-ip:5001`

---

### 2️⃣ Подключитесь к Registry с локальной машины

```bash
# Создайте SSH туннель
ssh -L 5000:localhost:5000 -N user@vps-ip &

# Войдите в registry
docker login localhost:5000
# Введите username/password из шага 1
```

---

### 3️⃣ Соберите и отправьте образ

**Windows:**

```powershell
.\scripts\build-and-push.ps1
```

**Linux/Mac:**

```bash
bash scripts/build-and-push.sh
```

**Результат:** Образ `localhost:5000/family-task-manager:latest` в registry

---

### 4️⃣ Подготовьте приложение на VPS

```bash
# На VPS
ssh user@vps-ip

# Создайте директорию
sudo mkdir -p /opt/family-task-manager && sudo chown $USER:$USER /opt/family-task-manager

# Скопируйте файлы (с локальной машины)
scp docker-compose.prod.yml scripts/deploy-from-registry.sh scripts/init-db.sql user@vps-ip:/opt/family-task-manager/

# Создайте .env (на VPS)
cd /opt/family-task-manager
cat > .env << 'EOF'
REGISTRY_HOST=localhost:5000
POSTGRES_USER=familytask
POSTGRES_PASSWORD=your-secure-password-here
TELEGRAM_BOT_TOKEN=your-bot-token-here
TELEGRAM_BOT_USERNAME=your_bot_username
EOF
```

---

### 5️⃣ Запустите деплой

```bash
# На VPS
cd /opt/family-task-manager
bash scripts/deploy-from-registry.sh
```

**Результат:** Приложение работает! ✅

---

## 🔄 Обновление приложения

```bash
# 1. Локально: соберите новый образ
ssh -L 5000:localhost:5000 -N user@vps-ip &
./scripts/build-and-push.ps1  # или .sh

# 2. На VPS: разверните
ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
```

---

## 🛠️ Полезные команды

### Проверка статуса

```bash
# Registry
docker compose -f /opt/docker-registry/docker-compose.registry.yml ps

# Приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps
```

### Логи

```bash
# Приложение
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f family-task-manager

# База данных
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f postgres
```

### Просмотр образов

```bash
# В registry
curl http://localhost:5000/v2/_catalog
curl http://localhost:5000/v2/family-task-manager/tags/list

# Или откройте UI: http://vps-ip:5001
```

### Бэкап БД

```bash
# Создается автоматически при каждом деплое в:
ls -lh /opt/backups/family-task-manager/

# Ручной бэкап
docker exec family-task-postgres pg_dump -U familytask FamilyTaskManager > backup.sql
```

---

## 🔧 Troubleshooting

### Registry недоступен

```bash
# Проверьте статус
docker ps | grep registry

# Перезапустите
cd /opt/docker-registry
docker compose -f docker-compose.registry.yml restart
```

### SSH туннель не работает

```bash
# Проверьте процесс
ps aux | grep "ssh.*5000"

# Убейте старый и создайте новый
pkill -f "ssh.*5000"
ssh -L 5000:localhost:5000 -N user@vps-ip &
```

### Приложение не запускается

```bash
# Проверьте логи
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs family-task-manager

# Проверьте .env файл
cat /opt/family-task-manager/.env

# Пересоздайте контейнер
docker compose -f /opt/family-task-manager/docker-compose.prod.yml up -d --force-recreate
```

---

## 📚 Дополнительная информация

- **Полная документация:** [docs/PRIVATE_REGISTRY_SETUP.md](docs/PRIVATE_REGISTRY_SETUP.md)
- **Настройка CI/CD:** См. раздел "GitHub Actions" в полной документации
- **Безопасность:** См. раздел "Security" в полной документации

---

## 💡 Советы

1. **Добавьте алиас SSH** для удобства:
   ```bash
   # ~/.ssh/config
   Host vps-registry
     HostName your-vps-ip
     User your-user
     LocalForward 5000 localhost:5000
   ```
   Теперь: `ssh vps-registry`

2. **Создайте скрипт для быстрого деплоя:**
   ```bash
   # deploy-quick.sh
   #!/bin/bash
   ssh -L 5000:localhost:5000 -N user@vps-ip &
   TUNNEL_PID=$!
   sleep 3
   ./scripts/build-and-push.ps1
   ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'
   kill $TUNNEL_PID
   ```

3. **Мониторинг места на диске:**
   ```bash
   # На VPS
   df -h
   docker system df
   
   # Очистка при необходимости
   docker system prune -a
   ```

---

## ✅ Чеклист первого деплоя

- [ ] Registry настроен и работает на VPS
- [ ] SSH туннель создан и работает
- [ ] Образ собран и отправлен в registry
- [ ] .env файл создан и заполнен на VPS
- [ ] Приложение запущено и работает
- [ ] Telegram бот отвечает на команды
- [ ] Бэкапы БД создаются автоматически

**Готово! 🎉**
