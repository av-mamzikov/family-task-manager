#!/bin/bash
# Скрипт для первоначальной настройки Private Docker Registry на VPS
# Запускать на VPS: bash setup-registry.sh

set -e

REGISTRY_DIR="/opt/docker-registry"
AUTH_DIR="$REGISTRY_DIR/registry-auth"

echo "=== Настройка Private Docker Registry ==="

# Создание директорий
echo "Создание директорий..."
sudo mkdir -p "$REGISTRY_DIR"
sudo mkdir -p "$AUTH_DIR"
sudo chown -R $USER:$USER "$REGISTRY_DIR"

# Установка htpasswd (если не установлен)
if ! command -v htpasswd &> /dev/null; then
    echo "Установка apache2-utils для htpasswd..."
    sudo apt update
    sudo apt install -y apache2-utils
fi

# Создание пользователя для registry
echo ""
echo "Создание пользователя для доступа к registry..."
read -p "Введите имя пользователя для registry: " REGISTRY_USER
htpasswd -Bc "$AUTH_DIR/htpasswd" "$REGISTRY_USER"

echo ""
echo "✓ Файл аутентификации создан: $AUTH_DIR/htpasswd"

# Копирование docker-compose файла
echo ""
echo "Убедитесь, что файл docker-compose.registry.yml находится в $REGISTRY_DIR"
echo "Если нет, скопируйте его:"
echo "  scp docker-compose.registry.yml user@vps:$REGISTRY_DIR/"

# Настройка Docker для работы с insecure registry (для локальной сети)
echo ""
echo "Настройка Docker daemon для работы с локальным registry..."
DAEMON_JSON="/etc/docker/daemon.json"

if [ -f "$DAEMON_JSON" ]; then
    echo "Файл $DAEMON_JSON уже существует. Добавьте вручную:"
else
    echo "Создание $DAEMON_JSON..."
    sudo tee "$DAEMON_JSON" > /dev/null <<EOF
{
  "insecure-registries": ["localhost:5000", "127.0.0.1:5000"]
}
EOF
fi

echo '{
  "insecure-registries": ["localhost:5000", "127.0.0.1:5000"]
}'

echo ""
echo "Перезапуск Docker..."
sudo systemctl restart docker

# Запуск registry
echo ""
echo "Запуск Docker Registry..."
cd "$REGISTRY_DIR"
docker compose -f docker-compose.registry.yml up -d

# Проверка
echo ""
echo "Проверка статуса registry..."
sleep 3
docker compose -f docker-compose.registry.yml ps

# Тест доступа
echo ""
echo "Тестирование registry..."
if curl -s http://localhost:5000/v2/_catalog > /dev/null; then
    echo "✓ Registry успешно запущен!"
else
    echo "✗ Ошибка: Registry не отвечает"
    exit 1
fi

echo ""
echo "=== Настройка завершена! ==="
echo ""
echo "Данные для входа:"
echo "  URL:      localhost:5000"
echo "  User:     $REGISTRY_USER"
echo "  Password: (тот, что вы ввели)"
echo ""
echo "Registry UI доступен по адресу: http://$(hostname -I | awk '{print $1}'):5001"
echo ""
echo "Для входа в registry с других машин:"
echo "  docker login localhost:5000 -u $REGISTRY_USER"
echo ""
echo "Следующий шаг: настройте CI/CD или локальную сборку для push образов в registry"
