# GitHub Actions Workflows

Автоматизация CI/CD для Family Task Manager.

## 📋 Список Workflows

| Workflow          | Файл                | Триггер     | Описание                |
|-------------------|---------------------|-------------|-------------------------|
| **Tests**         | `tests.yml`         | Push, PR    | Запуск unit тестов      |
| **Deploy to VPS** | `deploy.yml`        | Push в main | Тесты + Деплой на VPS   |
| **Code Coverage** | `code-coverage.yml` | Push, PR    | Измерение покрытия кода |
| **Code Quality**  | `code-quality.yml`  | Push, PR    | Проверка качества кода  |

## 🚀 Быстрый старт

### Для разработчика

1. **Создайте ветку и разработайте фичу**
   ```bash
   git checkout -b feature/my-feature
   # ... разработка ...
   ```

2. **Проверьте локально**
   ```bash
   dotnet test
   dotnet format
   ```

3. **Создайте PR**
   ```bash
   git push origin feature/my-feature
   # Создайте PR на GitHub
   ```

4. **Дождитесь проверок**
    - ✅ Tests
    - ✅ Code Quality
    - ✅ Code Coverage

5. **Мерж → Автоматический деплой!**

### Для первой настройки

1. **Добавьте GitHub Secrets** (`Settings` → `Secrets`):
    - `DOCKER_USERNAME`
    - `DOCKER_PASSWORD`
    - `VPS_HOST`
    - `VPS_USERNAME`
    - `VPS_SSH_KEY`

2. **Настройте VPS** (см. [DEPLOYMENT.md](../DEPLOYMENT.md))

3. **Запушьте в main** → деплой произойдёт автоматически

## 📊 Статус

Текущий статус workflows:

![Tests](https://github.com/YOUR_USERNAME/family-task-manager/workflows/Tests/badge.svg)
![Deploy](https://github.com/YOUR_USERNAME/family-task-manager/workflows/Deploy%20to%20VPS/badge.svg)

## 📚 Документация

- **[Руководство по Workflows](WORKFLOWS_GUIDE.md)** - как использовать
- **[CI/CD Pipeline](CI_CD.md)** - полная документация
- **[Шпаргалка](DEPLOYMENT_CHEATSHEET.md)** - команды для деплоя

## 🔧 Локальная проверка

Перед push проверьте локально:

```bash
# Тесты
dotnet test

# Форматирование
dotnet format --verify-no-changes

# Сборка с warnings as errors
dotnet build /p:TreatWarningsAsErrors=true
```

## 🐛 Troubleshooting

### Workflow не запускается

- Проверьте, что файл в `.github/workflows/`
- Проверьте синтаксис YAML
- Проверьте триггеры (on: push/pull_request)

### Тесты падают в CI

```bash
# Запустите локально с той же конфигурацией
docker run -d -p 5432:5432 \
  -e POSTGRES_DB=FamilyTaskManager_Test \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres_test_password \
  postgres:16-alpine

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=FamilyTaskManager_Test;Username=postgres;Password=postgres_test_password"
dotnet test
```

### Деплой не работает

1. Проверьте все секреты
2. Проверьте SSH доступ к VPS
3. Посмотрите логи в Actions

## 💡 Советы

- Всегда запускайте тесты локально перед push
- Используйте осмысленные commit messages
- Создавайте небольшие PR
- Следите за покрытием кода (> 80%)
- Не игнорируйте warnings

---

Подробнее: [WORKFLOWS_GUIDE.md](WORKFLOWS_GUIDE.md)
