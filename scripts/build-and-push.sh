#!/bin/bash
# Скрипт для сборки и отправки образа в Private Registry
# Запускать локально или на CI: bash build-and-push.sh

set -e

# Конфигурация
REGISTRY_HOST="${REGISTRY_HOST:-localhost:5000}"
IMAGE_NAME="family-task-manager"
FULL_IMAGE_NAME="$REGISTRY_HOST/$IMAGE_NAME"

# Получение версии из Git
GIT_COMMIT=$(git rev-parse --short HEAD)
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

echo "=== Сборка и отправка образа в Private Registry ==="
echo "Registry:  $REGISTRY_HOST"
echo "Image:     $IMAGE_NAME"
echo "Commit:    $GIT_COMMIT"
echo "Branch:    $GIT_BRANCH"
echo ""

# Проверка доступности registry
echo "Проверка доступности registry..."
if ! curl -sf "http://$REGISTRY_HOST/v2/_catalog" > /dev/null; then
    echo "✗ Ошибка: Registry недоступен по адресу $REGISTRY_HOST"
    echo "Убедитесь, что:"
    echo "  1. Registry запущен на VPS"
    echo "  2. Вы подключены к VPS через SSH tunnel:"
    echo "     ssh -L 5000:localhost:5000 user@vps-ip"
    exit 1
fi
echo "✓ Registry доступен"

# Вход в registry (если требуется)
if [ -n "$REGISTRY_USER" ] && [ -n "$REGISTRY_PASSWORD" ]; then
    echo ""
    echo "Вход в registry..."
    echo "$REGISTRY_PASSWORD" | docker login "$REGISTRY_HOST" -u "$REGISTRY_USER" --password-stdin
fi

# Сборка образа
echo ""
echo "Сборка Docker образа..."
docker build \
    --build-arg BUILD_CONFIGURATION=Release \
    --label "git.commit=$GIT_COMMIT" \
    --label "git.branch=$GIT_BRANCH" \
    --label "build.date=$BUILD_DATE" \
    --tag "$FULL_IMAGE_NAME:latest" \
    --tag "$FULL_IMAGE_NAME:$GIT_COMMIT" \
    --tag "$FULL_IMAGE_NAME:$GIT_BRANCH" \
    .

echo ""
echo "✓ Образ собран успешно"

# Отправка образа в registry
echo ""
echo "Отправка образа в registry..."
docker push "$FULL_IMAGE_NAME:latest"
docker push "$FULL_IMAGE_NAME:$GIT_COMMIT"
docker push "$FULL_IMAGE_NAME:$GIT_BRANCH"

echo ""
echo "✓ Образ успешно отправлен в registry!"
echo ""
echo "Доступные теги:"
echo "  - $FULL_IMAGE_NAME:latest"
echo "  - $FULL_IMAGE_NAME:$GIT_COMMIT"
echo "  - $FULL_IMAGE_NAME:$GIT_BRANCH"
echo ""
echo "Для деплоя на VPS выполните:"
echo "  ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'"
