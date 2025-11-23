#!/bin/bash
# Скрипт для ручного деплоя на сервере
# Использование: bash deploy.sh

set -e

cd /opt/family-task-manager

echo "=== Деплой Family Task Manager ==="

# Загрузка переменных окружения
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Остановка старых контейнеров
echo "Остановка старых контейнеров..."
docker compose down

# Удаление старых образов
echo "Удаление старых образов..."
docker image prune -f

# Загрузка нового образа
echo "Загрузка нового образа..."
docker compose pull

# Запуск новых контейнеров
echo "Запуск новых контейнеров..."
docker compose up -d

# Проверка статуса
echo ""
echo "Статус контейнеров:"
docker compose ps

echo ""
echo "=== Деплой завершен! ==="
echo ""
echo "Для просмотра логов: docker compose logs -f family-task-manager"
