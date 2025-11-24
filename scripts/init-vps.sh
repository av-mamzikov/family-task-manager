#!/bin/bash
# Единый скрипт инициализации VPS для Family Task Manager
# Использование: bash init-vps.sh
#
# Этот скрипт должен быть запущен на VPS от пользователя root

set -e

echo "=========================================="
echo "  Family Task Manager - VPS Setup"
echo "=========================================="
echo ""

# Проверка, что скрипт запущен от root
if [ "$EUID" -ne 0 ]; then 
    echo "❌ Ошибка: Этот скрипт должен быть запущен от root"
    echo "Используйте: sudo bash init-vps.sh"
    exit 1
fi

# Переменные
DEPLOY_USER="deploy"
PROJECT_DIR="/opt/family-task-manager"
REGISTRY_DIR="/opt/docker-registry"
AUTH_DIR="$REGISTRY_DIR/registry-auth"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VPS_IP=$(hostname -I | awk '{print $1}')

echo "📋 Конфигурация:"
echo "  - Пользователь для деплоя: $DEPLOY_USER"
echo "  - Директория проекта: $PROJECT_DIR"
echo "  - Директория registry: $REGISTRY_DIR"
echo ""

# Сбор данных от пользователя
echo "📝 Введите необходимые данные:"
echo ""

# SSH ключ администратора
echo "1️⃣  SSH ключ администратора (для вашего доступа к серверу)"
echo "   Вставьте ПУБЛИЧНЫЙ SSH ключ (например, содержимое ~/.ssh/id_ed25519.pub):"
read -r ADMIN_SSH_KEY
while [ -z "$ADMIN_SSH_KEY" ]; do
    echo "❌ Ключ не может быть пустым. Попробуйте снова:"
    read -r ADMIN_SSH_KEY
done
echo "✓ Ключ администратора сохранён"
echo ""

# SSH ключ GitHub Actions
echo "2️⃣  SSH ключ для GitHub Actions (для автоматического деплоя)"
echo "   Вставьте ПУБЛИЧНЫЙ SSH ключ (например, содержимое ~/.ssh/github_actions_key.pub):"
read -r GITHUB_ACTIONS_SSH_KEY
while [ -z "$GITHUB_ACTIONS_SSH_KEY" ]; do
    echo "❌ Ключ не может быть пустым. Попробуйте снова:"
    read -r GITHUB_ACTIONS_SSH_KEY
done
echo "✓ Ключ GitHub Actions сохранён"
echo ""

# Данные для Docker Registry
echo "3️⃣  Данные для Docker Registry"
read -p "   Имя пользователя для registry: " REGISTRY_USER
while [ -z "$REGISTRY_USER" ]; do
    echo "❌ Имя пользователя не может быть пустым"
    read -p "   Имя пользователя для registry: " REGISTRY_USER
done

read -sp "   Пароль для registry: " REGISTRY_PASSWORD
echo ""
while [ -z "$REGISTRY_PASSWORD" ]; do
    echo "❌ Пароль не может быть пустым"
    read -sp "   Пароль для registry: " REGISTRY_PASSWORD
    echo ""
done
echo "✓ Данные registry сохранены"
echo ""

echo "ℹ️  Данные PostgreSQL и Telegram бота будут установлены из GitHub Secrets при деплое"
echo ""

echo "=========================================="
echo "Начинаем настройку..."
echo "=========================================="
echo ""

# ============================================
# 1. Обновление системы
# ============================================
echo "🔄 Шаг 1/7: Обновление системы..."
apt-get update
apt-get upgrade -y

# ============================================
# 2. Установка необходимых пакетов
# ============================================
echo ""
echo "📦 Шаг 2/7: Установка необходимых пакетов..."
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    git \
    apache2-utils

# ============================================
# 3. Установка Docker
# ============================================
echo ""
echo "🐳 Шаг 3/7: Установка Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
    echo "✓ Docker установлен успешно"
else
    echo "✓ Docker уже установлен"
fi

# Установка Docker Compose
if ! command -v docker compose &> /dev/null; then
    DOCKER_COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep 'tag_name' | cut -d\" -f4)
    curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    echo "✓ Docker Compose установлен успешно"
else
    echo "✓ Docker Compose уже установлен"
fi

# ============================================
# 4. Создание пользователя deploy
# ============================================
echo ""
echo "👤 Шаг 4/7: Создание пользователя $DEPLOY_USER..."

if id "$DEPLOY_USER" &>/dev/null; then
    echo "✓ Пользователь $DEPLOY_USER уже существует"
else
    # Создаём пользователя без пароля (будет использоваться SSH ключ)
    useradd -m -s /bin/bash "$DEPLOY_USER"
    
    # Добавляем в группу sudo
    usermod -aG sudo "$DEPLOY_USER"
    
    # Добавляем в группу docker
    usermod -aG docker "$DEPLOY_USER"
    
    # Настраиваем sudo без пароля для deploy
    echo "$DEPLOY_USER ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/$DEPLOY_USER
    chmod 0440 /etc/sudoers.d/$DEPLOY_USER
    
    # Создаём директорию для SSH ключей
    mkdir -p /home/$DEPLOY_USER/.ssh
    chmod 700 /home/$DEPLOY_USER/.ssh
    touch /home/$DEPLOY_USER/.ssh/authorized_keys
    chmod 600 /home/$DEPLOY_USER/.ssh/authorized_keys
    chown -R $DEPLOY_USER:$DEPLOY_USER /home/$DEPLOY_USER/.ssh
    
    echo "✓ Пользователь $DEPLOY_USER создан успешно"
fi

# Добавление SSH ключей для deploy
echo ""
echo "🔑 Добавление SSH ключей для $DEPLOY_USER..."

# Ключ администратора
echo "$ADMIN_SSH_KEY" >> /home/$DEPLOY_USER/.ssh/authorized_keys
echo "✓ Добавлен ключ администратора"

# Ключ GitHub Actions
echo "$GITHUB_ACTIONS_SSH_KEY" >> /home/$DEPLOY_USER/.ssh/authorized_keys
echo "✓ Добавлен ключ GitHub Actions"

# Убираем дубликаты ключей
sort -u /home/$DEPLOY_USER/.ssh/authorized_keys -o /home/$DEPLOY_USER/.ssh/authorized_keys
chown $DEPLOY_USER:$DEPLOY_USER /home/$DEPLOY_USER/.ssh/authorized_keys

# ============================================
# 5. Создание директорий проекта
# ============================================
echo ""
echo "📁 Шаг 5/7: Создание директорий проекта..."
mkdir -p $PROJECT_DIR
mkdir -p $REGISTRY_DIR
mkdir -p $AUTH_DIR
chown -R $DEPLOY_USER:$DEPLOY_USER $PROJECT_DIR
chown -R $DEPLOY_USER:$DEPLOY_USER $REGISTRY_DIR

# Создание placeholder .env файла (будет перезаписан при деплое)
cat > $PROJECT_DIR/.env <<EOF
# Этот файл будет автоматически создан при деплое из GitHub Actions
# Данные берутся из GitHub Secrets
EOF

chown $DEPLOY_USER:$DEPLOY_USER $PROJECT_DIR/.env
chmod 600 $PROJECT_DIR/.env
echo "✓ Создан placeholder .env (будет обновлён при деплое)"

# ============================================
# 6. Настройка Private Docker Registry
# ============================================
echo ""
echo "🐳 Шаг 6/7: Настройка Private Docker Registry..."

# Создание пользователя для registry
echo "Создание пользователя registry: $REGISTRY_USER"
echo "$REGISTRY_PASSWORD" | htpasswd -Bci "$AUTH_DIR/htpasswd" "$REGISTRY_USER"
chown -R $DEPLOY_USER:$DEPLOY_USER $AUTH_DIR
echo "✓ Создан пользователь registry: $REGISTRY_USER"

# Создание docker-compose.yml
echo "Создание docker-compose.yml..."
cat > "$REGISTRY_DIR/docker-compose.yml" <<'COMPOSE_EOF'
services:
  registry:
    image: registry:2
    container_name: docker-registry
    restart: unless-stopped
    ports:
      - "5000:5000"
    environment:
      REGISTRY_AUTH: htpasswd
      REGISTRY_AUTH_HTPASSWD_REALM: "Registry Realm"
      REGISTRY_AUTH_HTPASSWD_PATH: /auth/htpasswd
      REGISTRY_STORAGE_FILESYSTEM_ROOTDIRECTORY: /var/lib/registry
      REGISTRY_STORAGE_DELETE_ENABLED: "true"
      REGISTRY_HTTP_HEADERS_Access__Control__Allow__Origin: "[*]"
      REGISTRY_HTTP_HEADERS_Access__Control__Allow__Methods: "[HEAD,GET,OPTIONS,DELETE]"
      REGISTRY_HTTP_HEADERS_Access__Control__Allow__Credentials: "[true]"
      REGISTRY_HTTP_HEADERS_Access__Control__Allow__Headers: "[Authorization,Accept,Cache-Control]"
      REGISTRY_HTTP_HEADERS_Access__Control__Expose__Headers: "[Docker-Content-Digest]"
    volumes:
      - registry_data:/var/lib/registry
      - ./registry-auth:/auth:ro
    networks:
      - registry-network
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  registry-ui:
    image: joxit/docker-registry-ui:latest
    container_name: docker-registry-ui
    restart: unless-stopped
    ports:
      - "5001:80"
    environment:
      - REGISTRY_TITLE=Family Task Manager Registry
      - REGISTRY_URL=http://registry:5000
      - DELETE_IMAGES=true
      - SHOW_CONTENT_DIGEST=true
      - SINGLE_REGISTRY=true
    depends_on:
      - registry
    networks:
      - registry-network
    logging:
      driver: "json-file"
      options:
        max-size: "5m"
        max-file: "2"

volumes:
  registry_data:
    driver: local

networks:
  registry-network:
    driver: bridge
COMPOSE_EOF

chown $DEPLOY_USER:$DEPLOY_USER "$REGISTRY_DIR/docker-compose.yml"
echo "✓ Создан docker-compose.yml"

# Настройка Docker daemon для работы с insecure registry
DAEMON_JSON="/etc/docker/daemon.json"
if [ ! -f "$DAEMON_JSON" ]; then
    echo "Создание $DAEMON_JSON..."
    cat > "$DAEMON_JSON" <<EOF
{
  "insecure-registries": ["localhost:5000", "127.0.0.1:5000"]
}
EOF
    systemctl restart docker
    echo "✓ Docker daemon настроен"
else
    echo "⚠️  Файл $DAEMON_JSON уже существует. Убедитесь, что добавлен insecure-registries"
fi

# Запуск registry
echo "Запуск Docker Registry..."
cd "$REGISTRY_DIR"
sudo -u $DEPLOY_USER docker compose up -d
sleep 3

# Проверка
if curl -s http://localhost:5000/v2/_catalog > /dev/null; then
    echo "✓ Registry успешно запущен!"
else
    echo "⚠️  Registry не отвечает. Проверьте логи: docker compose logs"
fi

# ============================================
# 7. Настройка Portainer (опционально)
# ============================================
echo ""
echo "🎛️  Шаг 7/8: Установка Portainer (опционально)..."
read -p "Установить Portainer для управления Docker? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    PORTAINER_DIR="/opt/portainer"
    mkdir -p "$PORTAINER_DIR"
    chown -R $DEPLOY_USER:$DEPLOY_USER "$PORTAINER_DIR"
    
    # Получаем GID группы docker для правильного доступа к socket
    DOCKER_GID=$(getent group docker | cut -d: -f3)
    
    echo "Создание docker-compose.yml для Portainer..."
    cat > "$PORTAINER_DIR/docker-compose.yml" <<PORTAINER_EOF
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
    group_add:
      - "${DOCKER_GID}"
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
PORTAINER_EOF
    
    chown $DEPLOY_USER:$DEPLOY_USER "$PORTAINER_DIR/docker-compose.yml"
    echo "✓ Добавлен GID группы docker ($DOCKER_GID) для доступа к socket"
    
    echo "Запуск Portainer..."
    cd "$PORTAINER_DIR"
    sudo -u $DEPLOY_USER docker compose up -d
    sleep 3
    
    if docker ps | grep -q portainer; then
        echo "✓ Portainer успешно запущен!"
        echo "  Доступ: http://$VPS_IP:9000 или https://$VPS_IP:9443"
    else
        echo "⚠️  Portainer не запустился. Проверьте логи: docker logs portainer"
    fi
else
    echo "⏭️  Пропущено"
fi

# ============================================
# 8. Настройка firewall (опционально)
# ============================================
echo ""
echo "🔥 Шаг 8/8: Настройка firewall (опционально)..."
read -p "Настроить UFW firewall? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    apt-get install -y ufw
    ufw --force enable
    ufw allow 22/tcp    # SSH
    ufw allow 80/tcp    # HTTP
    ufw allow 443/tcp   # HTTPS
    ufw allow 5000/tcp  # Docker Registry
    ufw allow 5001/tcp  # Registry UI
    ufw allow 9000/tcp  # Portainer HTTP
    ufw allow 9443/tcp  # Portainer HTTPS
    echo "✓ Firewall настроен"
else
    echo "⏭️  Пропущено"
fi

# ============================================
# Завершение
# ============================================
echo ""
echo "=========================================="
echo "  ✅ Настройка VPS завершена!"
echo "=========================================="
echo ""
echo "📝 Сохраните эти данные для GitHub Secrets:"
echo ""
echo "┌─────────────────────────────────────────────────────────"
echo "│ VPS_HOST              = $VPS_IP"
echo "│ VPS_USERNAME          = $DEPLOY_USER"
echo "│ VPS_SSH_KEY           = <приватный ключ github_actions_key>"
echo "│ REGISTRY_USERNAME     = $REGISTRY_USER"
echo "│ REGISTRY_PASSWORD     = $REGISTRY_PASSWORD"
echo "│"
echo "│ Также добавьте в GitHub Secrets:"
echo "│ POSTGRES_USER         = <имя пользователя БД>"
echo "│ POSTGRES_PASSWORD     = <пароль БД>"
echo "│ TELEGRAM_BOT_TOKEN    = <токен от @BotFather>"
echo "│ TELEGRAM_BOT_USERNAME = <username бота>"
echo "└─────────────────────────────────────────────────────────"
echo ""
echo "📝 Следующие шаги:"
echo ""
echo "1. Проверьте подключение от пользователя $DEPLOY_USER:"
echo "   ssh $DEPLOY_USER@$VPS_IP"
echo ""
echo "2. Настройте GitHub Secrets (используйте данные выше)"
echo "   Settings → Secrets and variables → Actions → New repository secret"
echo ""
echo "3. Проверьте статус registry:"
echo "   ssh $DEPLOY_USER@$VPS_IP"
echo "   cd $REGISTRY_DIR && docker compose ps"
echo ""
echo "4. Registry UI доступен по адресу:"
echo "   http://$VPS_IP:5001"
echo ""
if docker ps | grep -q portainer; then
echo "5. Portainer доступен по адресу:"
echo "   HTTP:  http://$VPS_IP:9000"
echo "   HTTPS: https://$VPS_IP:9443"
echo "   (При первом входе создайте администратора)"
echo ""
echo "6. Запушьте код в GitHub - деплой запустится автоматически!"
else
echo "5. Запушьте код в GitHub - деплой запустится автоматически!"
fi
echo ""
echo "📚 Документация: docs/setup/VPS_SETUP.md"
echo ""
