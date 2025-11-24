#!/bin/bash
# Скрипт первоначальной настройки сервера для Family Task Manager
# Использование: sudo bash server-setup.sh

set -e

echo "=== Настройка сервера для Family Task Manager ==="

# Обновление системы
echo "Обновление системы..."
apt-get update
apt-get upgrade -y

# Установка необходимых пакетов
echo "Установка необходимых пакетов..."
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    git

# Установка Docker
echo "Установка Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
    echo "Docker установлен успешно"
else
    echo "Docker уже установлен"
fi

# Установка Docker Compose
echo "Установка Docker Compose..."
if ! command -v docker compose &> /dev/null; then
    DOCKER_COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep 'tag_name' | cut -d\" -f4)
    curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    echo "Docker Compose установлен успешно"
else
    echo "Docker Compose уже установлен"
fi

# Создание пользователя для деплоя
echo "Создание пользователя deploy..."
DEPLOY_USER="deploy"

if id "$DEPLOY_USER" &>/dev/null; then
    echo "Пользователь $DEPLOY_USER уже существует"
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
    
    echo "Пользователь $DEPLOY_USER создан успешно"
    echo "⚠️  Добавьте SSH ключ в /home/$DEPLOY_USER/.ssh/authorized_keys"
fi

# Создание директорий для проекта
echo "Создание директорий проекта..."
mkdir -p /opt/family-task-manager
mkdir -p /opt/docker-registry
chown -R $DEPLOY_USER:$DEPLOY_USER /opt/family-task-manager
chown -R $DEPLOY_USER:$DEPLOY_USER /opt/docker-registry
cd /opt/family-task-manager

# Создание .env файла
echo "Создание .env файла..."
cat > .env << 'EOF'
# Docker Hub
DOCKER_USERNAME=your_dockerhub_username

# PostgreSQL
POSTGRES_USER=familytask
POSTGRES_PASSWORD=CHANGE_ME_STRONG_PASSWORD

# Telegram Bot
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_BOT_USERNAME=your_bot_username_here
EOF

# Создание docker-compose.yml (символическая ссылка будет создана позже)
echo "Создание структуры проекта..."
mkdir -p scripts

# Настройка автоматического перезапуска при перезагрузке
echo "Настройка автозапуска..."
cat > /etc/systemd/system/family-task-manager.service << 'EOF'
[Unit]
Description=Family Task Manager
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/family-task-manager
ExecStart=/usr/local/bin/docker-compose up -d
ExecStop=/usr/local/bin/docker-compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable family-task-manager.service

echo ""
echo "=== Настройка завершена! ==="
echo ""
echo "Следующие шаги:"
echo "1. Отредактируйте /opt/family-task-manager/.env и укажите ваши данные"
echo "2. Скопируйте docker-compose.prod.yml в /opt/family-task-manager/docker-compose.yml"
echo "3. Скопируйте scripts/init-db.sql в /opt/family-task-manager/scripts/"
echo "4. Настройте GitHub Secrets в вашем репозитории"
echo "5. Запустите: cd /opt/family-task-manager && docker compose up -d"
echo ""
echo "Для просмотра логов: docker compose logs -f"
echo "Для остановки: docker compose down"
