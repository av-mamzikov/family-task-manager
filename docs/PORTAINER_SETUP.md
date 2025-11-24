# 🐳 Portainer - Web UI для управления Docker

Portainer предоставляет удобный веб-интерфейс для управления Docker контейнерами на VPS.

## 🚀 Быстрая установка

### На VPS

```bash
# Создайте директорию
mkdir -p /opt/portainer
cd /opt/portainer

# Создайте docker-compose.yml
cat > docker-compose.yml << 'EOF'
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
    networks:
      - portainer-network
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

volumes:
  portainer_data:
    driver: local

networks:
  portainer-network:
    driver: bridge
EOF

# Запустите Portainer
docker compose up -d
```

### Или скопируйте из репозитория

```bash
# На вашем компьютере
scp docker-compose.portainer.yml root@ваш_ip:/opt/portainer/docker-compose.yml

# На VPS
ssh root@ваш_ip
cd /opt/portainer
docker compose up -d
```

## 🌐 Доступ

Откройте в браузере:

- **HTTP:** `http://ваш_ip:9000`
- **HTTPS:** `https://ваш_ip:9443`

При первом входе создайте admin аккаунт.

## ✨ Основные возможности

### 📊 Dashboard

- Обзор всех контейнеров
- Статистика использования ресурсов
- Быстрый доступ к управлению

### 🐳 Управление контейнерами

- **Start/Stop/Restart** - управление состоянием
- **Logs** - просмотр логов в реальном времени
- **Stats** - мониторинг CPU, RAM, Network, Disk I/O
- **Exec** - выполнение команд внутри контейнера через браузер
- **Inspect** - детальная информация о контейнере

### 📦 Управление образами

- Список всех образов
- Pull новых образов
- Удаление неиспользуемых образов
- Просмотр истории слоёв

### 💾 Volumes и Networks

- Управление Docker volumes
- Создание и удаление networks
- Просмотр использования

### 📚 Stacks (Docker Compose)

- Деплой через docker-compose.yml
- Редактирование compose файлов в UI
- Управление несколькими стеками

## 🔐 Безопасность

### Настройка firewall

Если хотите ограничить доступ к Portainer:

```bash
# Разрешить доступ только с вашего IP
sudo ufw allow from ваш_ip to any port 9000
sudo ufw allow from ваш_ip to any port 9443

# Или используйте SSH туннель
ssh -L 9000:localhost:9000 root@ваш_vps_ip
# Теперь доступ через http://localhost:9000
```

### HTTPS с Let's Encrypt

Portainer поддерживает автоматическое получение SSL сертификатов:

```bash
docker run -d \
  -p 9000:9000 \
  -p 9443:9443 \
  --name portainer \
  --restart=always \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce:latest \
  --sslcert /path/to/cert.pem \
  --sslkey /path/to/key.pem
```

## 📖 Полезные команды

```bash
# Проверить статус Portainer
docker ps | grep portainer

# Просмотр логов Portainer
docker logs portainer -f

# Перезапуск Portainer
docker restart portainer

# Обновление Portainer
cd /opt/portainer
docker compose pull
docker compose up -d

# Удаление Portainer
docker compose down
docker volume rm portainer_data
```

## 🎯 Типичные задачи

### Просмотр логов приложения

1. Откройте Portainer
2. Перейдите в **Containers**
3. Найдите `family-task-manager`
4. Нажмите на имя контейнера
5. Выберите **Logs**
6. Включите **Auto-refresh** для реального времени

### Перезапуск контейнера

1. **Containers** → найдите контейнер
2. Нажмите **Quick actions** → **Restart**

### Выполнение команды в контейнере

1. **Containers** → выберите контейнер
2. **Console** → выберите `/bin/sh` или `/bin/bash`
3. Нажмите **Connect**

### Мониторинг ресурсов

1. **Containers** → выберите контейнер
2. **Stats** → графики CPU, RAM, Network, I/O

## 🔄 Альтернативы

Если Portainer не подходит:

- **Dockge** - легковесная альтернатива для docker-compose
- **Lazydocker** - TUI в терминале
- **Yacht** - минималистичный UI
- **Cockpit** - системный мониторинг + Docker плагин

## 📚 Дополнительные ресурсы

- [Официальная документация](https://docs.portainer.io/)
- [GitHub](https://github.com/portainer/portainer)
- [Community Edition vs Business](https://www.portainer.io/pricing)

---

**Совет:** Portainer отлично подходит для начинающих и для быстрого управления контейнерами. Для production
рекомендуется также настроить мониторинг (Prometheus + Grafana) и логирование (ELK/Loki).
