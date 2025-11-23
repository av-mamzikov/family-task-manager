#!/bin/bash
# Скрипт для деплоя с локальной сборкой на VPS (без Docker Hub)
# Использование: bash deploy-build-on-vps.sh

set -e

APP_DIR="/opt/family-task-manager"
BACKUP_DIR="/opt/backups/family-task-manager"

cd "$APP_DIR"

echo "=== Деплой Family Task Manager (Build on VPS) ==="
echo "Время начала: $(date)"

# Загрузка переменных окружения
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Создание бэкапа БД перед обновлением
echo ""
echo "Создание бэкапа базы данных..."
mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/db_backup_$(date +%Y%m%d_%H%M%S).sql"
docker exec family-task-postgres pg_dump -U "${POSTGRES_USER}" FamilyTaskManager > "$BACKUP_FILE" 2>/dev/null || echo "Предупреждение: не удалось создать бэкап (возможно, это первый запуск)"

# Получение последних изменений из Git
echo ""
echo "Получение последних изменений из репозитория..."
git fetch origin
CURRENT_COMMIT=$(git rev-parse HEAD)
LATEST_COMMIT=$(git rev-parse origin/main)

if [ "$CURRENT_COMMIT" = "$LATEST_COMMIT" ]; then
    echo "Уже на последней версии ($CURRENT_COMMIT)"
    read -p "Продолжить деплой? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Деплой отменен"
        exit 0
    fi
else
    echo "Обновление с $CURRENT_COMMIT на $LATEST_COMMIT"
    git pull origin main
fi

# Остановка старых контейнеров (кроме БД)
echo ""
echo "Остановка приложения..."
docker compose -f docker-compose.prod.yml stop family-task-manager

# Сборка нового образа
echo ""
echo "Сборка Docker образа..."
docker build \
    --build-arg BUILD_CONFIGURATION=Release \
    --tag family-task-manager:latest \
    --tag family-task-manager:$(git rev-parse --short HEAD) \
    .

# Очистка старых образов
echo ""
echo "Очистка неиспользуемых образов..."
docker image prune -f

# Запуск обновленного приложения
echo ""
echo "Запуск обновленного приложения..."
docker compose -f docker-compose.prod.yml up -d

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

# Проверка здоровья
echo ""
if docker compose -f docker-compose.prod.yml ps | grep -q "family-task-manager.*Up"; then
    echo "✓ Приложение успешно запущено"
    echo "✓ Бэкап БД сохранен: $BACKUP_FILE"
    echo ""
    echo "=== Деплой завершен успешно! ==="
else
    echo "✗ ОШИБКА: Приложение не запустилось!"
    echo "Для отката выполните: git reset --hard $CURRENT_COMMIT && bash deploy-build-on-vps.sh"
    exit 1
fi

echo ""
echo "Полезные команды:"
echo "  Логи:        docker compose -f docker-compose.prod.yml logs -f family-task-manager"
echo "  Рестарт:     docker compose -f docker-compose.prod.yml restart family-task-manager"
echo "  Остановка:   docker compose -f docker-compose.prod.yml down"
echo "  Бэкапы БД:   ls -lh $BACKUP_DIR"
echo ""
