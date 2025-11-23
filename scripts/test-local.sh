#!/bin/bash
# Скрипт для быстрого локального тестирования деплоя
# Использование: bash scripts/test-local.sh

set -e

echo "=== Локальное тестирование Family Task Manager ==="
echo ""

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Проверка наличия Docker
echo -e "${YELLOW}Проверка Docker...${NC}"
if ! command -v docker &> /dev/null; then
    echo -e "${RED}ОШИБКА: Docker не установлен!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker установлен${NC}"

# Проверка наличия .env файла
echo ""
echo -e "${YELLOW}Проверка .env файла...${NC}"
if [ ! -f ".env" ]; then
    echo -e "${YELLOW}ПРЕДУПРЕЖДЕНИЕ: .env файл не найден.${NC}"
    echo -n "Создать .env файл из .env.example? (y/n): "
    read -r response
    if [ "$response" = "y" ]; then
        cp .env.example .env
        echo -e "${GREEN}✓ Создан .env файл. Отредактируйте его перед продолжением!${NC}"
        ${EDITOR:-nano} .env
        echo "Нажмите Enter после редактирования .env..."
        read -r
    else
        echo -e "${RED}Тестирование прервано. Создайте .env файл и запустите скрипт снова.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ .env файл найден${NC}"
fi

# Шаг 1: Сборка образа
echo ""
echo -e "${CYAN}=== Шаг 1: Сборка Docker образа ===${NC}"
docker build -t family-task-manager:test .
echo -e "${GREEN}✓ Образ успешно собран${NC}"

# Шаг 2: Проверка размера образа
echo ""
echo -e "${CYAN}=== Шаг 2: Информация об образе ===${NC}"
docker images family-task-manager:test
IMAGE_SIZE=$(docker images family-task-manager:test --format "{{.Size}}")
echo -e "${YELLOW}Размер образа: $IMAGE_SIZE${NC}"

# Шаг 3: Запуск с production конфигурацией
echo ""
echo -e "${CYAN}=== Шаг 3: Запуск с production конфигурацией ===${NC}"

# Сначала остановим, если что-то запущено
docker compose -f docker-compose.prod.yml down 2>/dev/null || true

# Тегируем образ для production compose
docker tag family-task-manager:test test/family-task-manager:latest

echo -e "${YELLOW}Запуск контейнеров...${NC}"
docker compose -f docker-compose.prod.yml up -d

# Ждём запуска
echo -e "${YELLOW}Ожидание запуска контейнеров (15 секунд)...${NC}"
sleep 15

# Шаг 4: Проверка статуса
echo ""
echo -e "${CYAN}=== Шаг 4: Статус контейнеров ===${NC}"
docker compose -f docker-compose.prod.yml ps

# Шаг 5: Проверка логов
echo ""
echo -e "${CYAN}=== Шаг 5: Логи приложения (последние 20 строк) ===${NC}"
docker compose -f docker-compose.prod.yml logs --tail=20 family-task-manager

# Шаг 6: Проверка здоровья БД
echo ""
echo -e "${CYAN}=== Шаг 6: Проверка PostgreSQL ===${NC}"
DB_HEALTH=$(docker inspect family-task-postgres --format='{{.State.Health.Status}}' 2>/dev/null || echo "unknown")
if [ "$DB_HEALTH" = "healthy" ]; then
    echo -e "${GREEN}✓ PostgreSQL работает корректно${NC}"
else
    echo -e "${YELLOW}ПРЕДУПРЕЖДЕНИЕ: PostgreSQL не в статусе healthy (текущий: $DB_HEALTH)${NC}"
fi

# Шаг 7: Интерактивное тестирование
echo ""
echo -e "${CYAN}=== Шаг 7: Тестирование бота ===${NC}"
echo -e "${YELLOW}Откройте Telegram и отправьте команду /start вашему боту${NC}"
echo ""
echo -e "${CYAN}Выберите действие:${NC}"
echo "1. Показать логи в реальном времени"
echo "2. Проверить таблицы БД"
echo "3. Остановить контейнеры и завершить"
echo "4. Оставить контейнеры запущенными и завершить"
echo ""
echo -n "Ваш выбор (1-4): "
read -r choice

case $choice in
    1)
        echo ""
        echo -e "${YELLOW}Показ логов в реальном времени (Ctrl+C для выхода)...${NC}"
        echo ""
        docker compose -f docker-compose.prod.yml logs -f family-task-manager
        ;;
    2)
        echo ""
        echo -e "${YELLOW}Проверка таблиц БД...${NC}"
        docker exec -it family-task-postgres psql -U familytask -d FamilyTaskManager -c "\dt"
        echo ""
        echo "Нажмите Enter для продолжения..."
        read -r
        ;;
    3)
        echo ""
        echo -e "${YELLOW}Остановка контейнеров...${NC}"
        docker compose -f docker-compose.prod.yml down
        echo -e "${GREEN}✓ Контейнеры остановлены${NC}"
        ;;
    4)
        echo ""
        echo -e "${GREEN}Контейнеры оставлены запущенными${NC}"
        echo -e "${YELLOW}Для остановки выполните: docker compose -f docker-compose.prod.yml down${NC}"
        ;;
    *)
        echo -e "${YELLOW}Неверный выбор. Контейнеры оставлены запущенными.${NC}"
        ;;
esac

# Итоги
echo ""
echo -e "${CYAN}=== Итоги тестирования ===${NC}"
echo ""
echo -e "${YELLOW}Полезные команды:${NC}"
echo "  Логи:      docker compose -f docker-compose.prod.yml logs -f"
echo "  Статус:    docker compose -f docker-compose.prod.yml ps"
echo "  Остановка: docker compose -f docker-compose.prod.yml down"
echo "  Перезапуск: docker compose -f docker-compose.prod.yml restart"
echo ""
echo -e "${GREEN}Если всё работает корректно, можно деплоить на VPS!${NC}"
echo ""
