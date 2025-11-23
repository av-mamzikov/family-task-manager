# Скрипт для быстрого локального тестирования деплоя
# Использование: .\scripts\test-local.ps1

Write-Host "=== Локальное тестирование Family Task Manager ===" -ForegroundColor Cyan
Write-Host ""

# Проверка наличия Docker
Write-Host "Проверка Docker..." -ForegroundColor Yellow
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "ОШИБКА: Docker не установлен!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Docker установлен" -ForegroundColor Green

# Проверка наличия .env файла
Write-Host ""
Write-Host "Проверка .env файла..." -ForegroundColor Yellow
if (-not (Test-Path ".env")) {
    Write-Host "ПРЕДУПРЕЖДЕНИЕ: .env файл не найден. Создайте его из .env.example" -ForegroundColor Yellow
    Write-Host "Создать .env файл сейчас? (y/n): " -NoNewline
    $response = Read-Host
    if ($response -eq "y") {
        Copy-Item ".env.example" ".env"
        Write-Host "✓ Создан .env файл. Отредактируйте его перед продолжением!" -ForegroundColor Green
        notepad .env
        Write-Host "Нажмите Enter после редактирования .env..." -NoNewline
        Read-Host
    } else {
        Write-Host "Тестирование прервано. Создайте .env файл и запустите скрипт снова." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✓ .env файл найден" -ForegroundColor Green
}

# Шаг 1: Сборка образа
Write-Host ""
Write-Host "=== Шаг 1: Сборка Docker образа ===" -ForegroundColor Cyan
docker build -t family-task-manager:test .
if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось собрать образ!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Образ успешно собран" -ForegroundColor Green

# Шаг 2: Проверка размера образа
Write-Host ""
Write-Host "=== Шаг 2: Информация об образе ===" -ForegroundColor Cyan
docker images family-task-manager:test
$imageSize = docker images family-task-manager:test --format "{{.Size}}"
Write-Host "Размер образа: $imageSize" -ForegroundColor Yellow

# Шаг 3: Запуск с production конфигурацией
Write-Host ""
Write-Host "=== Шаг 3: Запуск с production конфигурацией ===" -ForegroundColor Cyan

# Сначала остановим, если что-то запущено
docker compose -f docker-compose.prod.yml down 2>$null

# Тегируем образ для production compose
docker tag family-task-manager:test test/family-task-manager:latest

Write-Host "Запуск контейнеров..." -ForegroundColor Yellow
docker compose -f docker-compose.prod.yml up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось запустить контейнеры!" -ForegroundColor Red
    exit 1
}

# Ждём запуска
Write-Host "Ожидание запуска контейнеров (15 секунд)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Шаг 4: Проверка статуса
Write-Host ""
Write-Host "=== Шаг 4: Статус контейнеров ===" -ForegroundColor Cyan
docker compose -f docker-compose.prod.yml ps

# Шаг 5: Проверка логов
Write-Host ""
Write-Host "=== Шаг 5: Логи приложения (последние 20 строк) ===" -ForegroundColor Cyan
docker compose -f docker-compose.prod.yml logs --tail=20 family-task-manager

# Шаг 6: Проверка здоровья БД
Write-Host ""
Write-Host "=== Шаг 6: Проверка PostgreSQL ===" -ForegroundColor Cyan
$dbHealth = docker inspect family-task-postgres --format='{{.State.Health.Status}}' 2>$null
if ($dbHealth -eq "healthy") {
    Write-Host "✓ PostgreSQL работает корректно" -ForegroundColor Green
} else {
    Write-Host "ПРЕДУПРЕЖДЕНИЕ: PostgreSQL не в статусе healthy (текущий: $dbHealth)" -ForegroundColor Yellow
}

# Шаг 7: Интерактивное тестирование
Write-Host ""
Write-Host "=== Шаг 7: Тестирование бота ===" -ForegroundColor Cyan
Write-Host "Откройте Telegram и отправьте команду /start вашему боту" -ForegroundColor Yellow
Write-Host ""
Write-Host "Выберите действие:" -ForegroundColor Cyan
Write-Host "1. Показать логи в реальном времени"
Write-Host "2. Проверить таблицы БД"
Write-Host "3. Остановить контейнеры и завершить"
Write-Host "4. Оставить контейнеры запущенными и завершить"
Write-Host ""
Write-Host "Ваш выбор (1-4): " -NoNewline
$choice = Read-Host

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Показ логов в реальном времени (Ctrl+C для выхода)..." -ForegroundColor Yellow
        Write-Host ""
        docker compose -f docker-compose.prod.yml logs -f family-task-manager
    }
    "2" {
        Write-Host ""
        Write-Host "Проверка таблиц БД..." -ForegroundColor Yellow
        docker exec -it family-task-postgres psql -U familytask -d FamilyTaskManager -c "\dt"
        Write-Host ""
        Write-Host "Нажмите Enter для продолжения..." -NoNewline
        Read-Host
    }
    "3" {
        Write-Host ""
        Write-Host "Остановка контейнеров..." -ForegroundColor Yellow
        docker compose -f docker-compose.prod.yml down
        Write-Host "✓ Контейнеры остановлены" -ForegroundColor Green
    }
    "4" {
        Write-Host ""
        Write-Host "Контейнеры оставлены запущенными" -ForegroundColor Green
        Write-Host "Для остановки выполните: docker compose -f docker-compose.prod.yml down" -ForegroundColor Yellow
    }
    default {
        Write-Host "Неверный выбор. Контейнеры оставлены запущенными." -ForegroundColor Yellow
    }
}

# Итоги
Write-Host ""
Write-Host "=== Итоги тестирования ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Полезные команды:" -ForegroundColor Yellow
Write-Host "  Логи:      docker compose -f docker-compose.prod.yml logs -f" -ForegroundColor Gray
Write-Host "  Статус:    docker compose -f docker-compose.prod.yml ps" -ForegroundColor Gray
Write-Host "  Остановка: docker compose -f docker-compose.prod.yml down" -ForegroundColor Gray
Write-Host "  Перезапуск: docker compose -f docker-compose.prod.yml restart" -ForegroundColor Gray
Write-Host ""
Write-Host "Если всё работает корректно, можно деплоить на VPS!" -ForegroundColor Green
Write-Host ""
