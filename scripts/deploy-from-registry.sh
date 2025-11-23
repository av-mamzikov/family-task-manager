#!/bin/bash
# Скрипт для деплоя из Private Registry на VPS
# Запускать на VPS: bash deploy-from-registry.sh

set -e

APP_DIR="/opt/family-task-manager"
BACKUP_DIR="/opt/backups/family-task-manager"
REGISTRY_HOST="localhost:5000"
IMAGE_NAME="$REGISTRY_HOST/family-task-manager"

cd "$APP_DIR"

echo "=== Деплой Family Task Manager из Private Registry ==="
echo "Время начала: $(date)"

# Загрузка переменных окружения
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Проверка доступности registry
echo ""
echo "Проверка доступности registry..."
if ! curl -sf "http://$REGISTRY_HOST/v2/_catalog" > /dev/null; then
    echo "✗ Ошибка: Registry недоступен"
    exit 1
fi
echo "✓ Registry доступен"

# Создание бэкапа БД перед обновлением
echo ""
echo "Создание бэкапа базы данных..."
mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/db_backup_$(date +%Y%m%d_%H%M%S).sql"
docker exec family-task-postgres pg_dump -U "${POSTGRES_USER}" FamilyTaskManager > "$BACKUP_FILE" 2>/dev/null || echo "Предупреждение: не удалось создать бэкап (возможно, это первый запуск)"

# Получение информации о текущем образе
echo ""
echo "Текущий образ:"
docker images "$IMAGE_NAME" --format "table {{.Repository}}:{{.Tag}}\t{{.ID}}\t{{.CreatedAt}}" | head -2

# Загрузка нового образа из registry
echo ""
echo "Загрузка нового образа из registry..."
docker pull "$IMAGE_NAME:latest"

# Проверка, изменился ли образ
NEW_IMAGE_ID=$(docker images "$IMAGE_NAME:latest" --format "{{.ID}}")
CURRENT_IMAGE_ID=$(docker inspect family-task-manager --format='{{.Image}}' 2>/dev/null | cut -d':' -f2 || echo "none")

if [ "$NEW_IMAGE_ID" = "$CURRENT_IMAGE_ID" ]; then
    echo ""
    echo "Образ не изменился (ID: $NEW_IMAGE_ID)"
    read -p "Продолжить деплой? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Деплой отменен"
        exit 0
    fi
else
    echo "Новый образ загружен (ID: $NEW_IMAGE_ID)"
fi

# Остановка старого контейнера
echo ""
echo "Остановка приложения..."
docker compose -f docker-compose.prod.yml stop family-task-manager

# Удаление старого контейнера
echo "Удаление старого контейнера..."
docker compose -f docker-compose.prod.yml rm -f family-task-manager

# Запуск нового контейнера
echo ""
echo "Запуск обновленного приложения..."
docker compose -f docker-compose.prod.yml up -d family-task-manager

# Ожидание запуска
echo ""
echo "Ожидание запуска приложения..."
sleep 10

# Проверка статуса
echo ""
echo "Статус контейнеров:"
docker compose -f docker-compose.prod.yml ps

# Проверка логов на наличие ошибок
echo ""
echo "Последние логи приложения:"
docker compose -f docker-compose.prod.yml logs --tail=50 family-task-manager

# Очистка старых образов
echo ""
echo "Очистка неиспользуемых образов..."
docker image prune -f

# Проверка здоровья
echo ""
if docker compose -f docker-compose.prod.yml ps | grep -q "family-task-manager.*Up"; then
    echo "✓ Приложение успешно запущено"
    echo "✓ Бэкап БД сохранен: $BACKUP_FILE"
    echo ""
    echo "=== Деплой завершен успешно! ==="
    
    # Показать информацию об образе
    echo ""
    echo "Информация о развернутом образе:"
    docker inspect "$IMAGE_NAME:latest" --format='
    Commit:  {{index .Config.Labels "git.commit"}}
    Branch:  {{index .Config.Labels "git.branch"}}
    Built:   {{index .Config.Labels "build.date"}}
    Image:   {{.Id}}' 2>/dev/null || echo "Метаданные недоступны"
else
    echo "✗ ОШИБКА: Приложение не запустилось!"
    echo "Проверьте логи: docker compose -f docker-compose.prod.yml logs family-task-manager"
    exit 1
fi

echo ""
echo "Полезные команды:"
echo "  Логи:        docker compose -f docker-compose.prod.yml logs -f family-task-manager"
echo "  Рестарт:     docker compose -f docker-compose.prod.yml restart family-task-manager"
echo "  Остановка:   docker compose -f docker-compose.prod.yml down"
echo "  Бэкапы БД:   ls -lh $BACKUP_DIR"
echo "  Образы:      docker images $IMAGE_NAME"
echo ""
