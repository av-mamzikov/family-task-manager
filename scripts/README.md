# Scripts для первоначальной настройки VPS

Эти скрипты используются **только один раз** при первоначальной настройке VPS.

## server-setup.sh

Устанавливает необходимое ПО на чистый VPS:
- Docker и Docker Compose
- Базовые утилиты (curl, git и т.д.)
- Настройка системы

**Использование:**
```bash
sudo bash server-setup.sh
```

## setup-registry.sh

Настраивает Private Docker Registry на VPS:
- Создаёт и запускает registry контейнер
- Настраивает аутентификацию
- Конфигурирует порты и volumes

**Использование:**
```bash
bash setup-registry.sh
```

---

## Автоматический деплой

После первоначальной настройки VPS, все деплои происходят автоматически через **GitHub Actions**:
- `.github/workflows/tests.yml` - запуск тестов
- `.github/workflows/deploy-registry.yml` - сборка и деплой

Ручные скрипты для деплоя больше не нужны.
