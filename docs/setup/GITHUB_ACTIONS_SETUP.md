# ⚙️ CI/CD и PR Preview

## Workflows GitHub Actions

### `Tests` (`.github/workflows/tests.yml`)

- Запускается на pull request и пуши в основные ветки.
- Собирает проект и прогоняет тесты.
- Является обязательным шагом перед автодеплоем на VPS.

### `Deploy to VPS` (`.github/workflows/deploy-registry.yml`)

Запускается в трёх сценариях:

- **После успешных тестов** на ветках `main`/`master` (`workflow_run`) — деплой в production.
- **На pull request** c меткой `deploy-preview` — деплой PR Preview окружения для этого PR.
- **Вручную** через `Run workflow` (`workflow_dispatch`) — можно выбрать окружение (`production` или `pr-preview`).

## PR Preview окружение

PR Preview — это отдельное окружение для проверки изменений из pull request:

- Использует отдельные секреты:
  - `PR_BOT_TOKEN`, `PR_BOT_USERNAME` — тестовый Telegram-бот.
  - `PR_POSTGRES_USER`, `PR_POSTGRES_PASSWORD` — учётные данные тестовой БД.
- Бот и БД **изолированы от production**.
- Имя БД для PR: `FamilyTaskManager_PR_<номер_PR>`.

## Метки PR

### `deploy-preview`

- Если на PR добавлена метка `deploy-preview`:
  - Запускается workflow `Deploy to VPS` в режиме PR Preview.
  - Собирается Docker-образ с тегом `pr-<номер_PR>` и деплоится на VPS в директорию preview.

### `clean-db`

Метка `clean-db` управляет тем, будет ли очищена база данных PR Preview при деплое.

- **Если метка `clean-db` есть на PR**:
  - Перед деплоем GitHub Actions **полностью очищает предыдущее preview окружение** для всех PR:
    - останавливает и удаляет контейнеры `family-task-manager-pr-*` и `family-task-postgres-pr-*`;
    - удаляет volumes `postgres_data_pr_*`;
    - удаляет сети `family-task-network-pr-*`.
  - После этого поднимается **чистая** БД для текущего PR.
- **Если метки `clean-db` нет**:
  - Preview-деплой **сохраняет существующую БД** для этого PR:
    - контейнеры/volume/сети не пересоздаются,
    - обновляется только образ приложения.

### Комментарий в PR

После успешного деплоя PR Preview workflow добавляет комментарий в PR, в котором указано:

- тег образа (например, `pr-123`),
- была ли выполнена очистка БД:
  - `Database reset: Yes (clean-db label is set)` — БД очищена перед деплоем;
  - `Database reset: No (database preserved)` — БД сохранена.

## Секреты, используемые в workflows

Ниже перечислены **все GitHub Secrets**, которые используются в пайплайнах:

| Secret                | Где используется                            | Для чего                                           |
|-----------------------|---------------------------------------------|----------------------------------------------------|
| `VPS_HOST`            | `Deploy to VPS` (`deploy-registry.yml`)     | Адрес VPS для SSH-подключения                      |
| `VPS_USERNAME`        | `Deploy to VPS`                             | Пользователь SSH (обычно `deploy`)                |
| `VPS_SSH_KEY`         | `Deploy to VPS`                             | Приватный SSH-ключ, которым GitHub Actions ходит на VPS |
| `REGISTRY_USERNAME`   | `Deploy to VPS`                             | Логин для приватного Docker Registry на VPS        |
| `REGISTRY_PASSWORD`   | `Deploy to VPS`                             | Пароль для приватного Docker Registry              |
| `TELEGRAM_BOT_TOKEN`  | `Deploy to VPS` (production)                | Токен production Telegram-бота                     |
| `TELEGRAM_BOT_USERNAME` | `Deploy to VPS` (production)              | Username production-бота (БЕЗ @)                   |
| `POSTGRES_USER`       | `Deploy to VPS` (production)                | Пользователь PostgreSQL для production БД          |
| `POSTGRES_PASSWORD`   | `Deploy to VPS` (production)                | Пароль PostgreSQL для production БД                |
| `PR_BOT_TOKEN`        | `Deploy to VPS` (PR Preview)                | Токен тестового Telegram-бота для PR Preview       |
| `PR_BOT_USERNAME`     | `Deploy to VPS` (PR Preview)                | Username тестового бота (БЕЗ @) для PR Preview     |
| `PR_POSTGRES_USER`    | `Deploy to VPS` (PR Preview)                | Пользователь PostgreSQL для тестовой БД PR Preview |
| `PR_POSTGRES_PASSWORD`| `Deploy to VPS` (PR Preview)                | Пароль PostgreSQL для тестовой БД PR Preview       |
| `DOCKERHUB_USERNAME`  | `Deploy to VPS` (опционально)               | Username Docker Hub для авторизованных pull-ов     |
| `DOCKERHUB_PASSWORD`  | `Deploy to VPS` (опционально)               | Access Token Docker Hub                            |

> 🔎 **Подробнее про формат значений и как создать эти секреты** см. в [Secrets Setup](SECRETS_SETUP.md).
