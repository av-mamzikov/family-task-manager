# Настройка GitHub Actions для Private Registry

Инструкция по настройке автоматического CI/CD через GitHub Actions с использованием Private Docker Registry.

## 📋 Содержание

- [Обзор workflow](#обзор-workflow)
- [Настройка GitHub Secrets](#настройка-github-secrets)
- [Активация workflow](#активация-workflow)
- [Проверка работы](#проверка-работы)
- [Troubleshooting](#troubleshooting)

---

## Обзор workflow

### deploy-registry.yml (Рекомендуется) ⭐

**Файл:** `.github/workflows/deploy-registry.yml`

**Процесс:**

1. ✅ Запуск тестов
2. 🔨 Сборка Docker образа
3. 📤 Push в Private Registry через SSH туннель
4. 🚀 Деплой на VPS из registry

**Преимущества:**

- ⚡ Быстрый деплой (~2-3 минуты)
- 🔒 Полная приватность
- 📦 Версионирование образов
- 🔄 Легкий откат

### deploy.yml (Legacy)

**Файл:** `.github/workflows/deploy.yml`

**Статус:** Устаревший, отключен по умолчанию

**Процесс:** docker save → scp → docker load (медленно)

---

## Настройка GitHub Secrets

### 1. Перейдите в настройки репозитория

```
GitHub Repository → Settings → Secrets and variables → Actions
```

### 2. Создайте следующие secrets

#### VPS доступ

**`VPS_HOST`**

- **Описание:** IP адрес или домен вашего VPS
- **Пример:** `123.45.67.89` или `vps.example.com`
- **Как получить:**
  ```bash
  # На VPS
  curl ifconfig.me
  ```

**`VPS_USERNAME`**

- **Описание:** Имя пользователя для SSH подключения
- **Пример:** `ubuntu`, `root`, `deploy`
- **Рекомендация:** Используйте непривилегированного пользователя с sudo

**`VPS_SSH_KEY`**

- **Описание:** Приватный SSH ключ для подключения к VPS
- **Как получить:**
  ```bash
  # На локальной машине
  cat ~/.ssh/id_rsa
  # Или создайте новый ключ специально для GitHub Actions:
  ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_key
  cat ~/.ssh/github_actions_key
  ```
- **Важно:** Скопируйте весь ключ, включая заголовки:
  ```
  -----BEGIN OPENSSH PRIVATE KEY-----
  ...
  -----END OPENSSH PRIVATE KEY-----
  ```
- **Настройка на VPS:**
  ```bash
  # Добавьте публичный ключ на VPS
  cat ~/.ssh/github_actions_key.pub | ssh user@vps 'cat >> ~/.ssh/authorized_keys'
  ```

#### Registry доступ

**`REGISTRY_USERNAME`**

- **Описание:** Имя пользователя для Private Registry
- **Пример:** `deploy-user`
- **Как получить:** Это имя, которое вы создали при запуске `setup-registry.sh`

**`REGISTRY_PASSWORD`**

- **Описание:** Пароль для Private Registry
- **Важно:** Используйте сильный пароль!
- **Как получить:** Пароль, который вы ввели при запуске `setup-registry.sh`

---

## Пошаговая настройка

### Шаг 1: Создание SSH ключа для GitHub Actions

```bash
# На локальной машине
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_actions_key -N ""

# Выведет два файла:
# ~/.ssh/github_actions_key      (приватный - для GitHub Secret)
# ~/.ssh/github_actions_key.pub  (публичный - для VPS)
```

### Шаг 2: Добавление публичного ключа на VPS

```bash
# Скопируйте публичный ключ на VPS
ssh-copy-id -i ~/.ssh/github_actions_key.pub user@vps-ip

# Или вручную:
cat ~/.ssh/github_actions_key.pub | ssh user@vps-ip 'mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys'

# Проверьте подключение
ssh -i ~/.ssh/github_actions_key user@vps-ip 'echo "Connection successful!"'
```

### Шаг 3: Добавление secrets в GitHub

1. Откройте репозиторий на GitHub
2. Перейдите: **Settings** → **Secrets and variables** → **Actions**
3. Нажмите **New repository secret**
4. Добавьте каждый secret:

| Name                | Value              | Example                 |
|---------------------|--------------------|-------------------------|
| `VPS_HOST`          | IP адрес VPS       | `123.45.67.89`          |
| `VPS_USERNAME`      | SSH пользователь   | `ubuntu`                |
| `VPS_SSH_KEY`       | Приватный SSH ключ | `-----BEGIN OPENSSH...` |
| `REGISTRY_USERNAME` | Registry user      | `deploy-user`           |
| `REGISTRY_PASSWORD` | Registry password  | `your-secure-password`  |

### Шаг 4: Проверка secrets

```bash
# На VPS проверьте, что authorized_keys содержит ключ
cat ~/.ssh/authorized_keys | grep "github-actions-deploy"

# Проверьте права доступа
ls -la ~/.ssh/
# Должно быть:
# drwx------  .ssh/
# -rw-------  authorized_keys
```

---

## Активация workflow

### Вариант 1: Автоматический запуск при push

Workflow уже настроен на автоматический запуск при push в `main` или `master`.

```bash
# Просто сделайте commit и push
git add .
git commit -m "Enable GitHub Actions deployment"
git push origin main
```

### Вариант 2: Ручной запуск

1. Перейдите в репозиторий на GitHub
2. Откройте вкладку **Actions**
3. Выберите **Deploy to VPS via Private Registry**
4. Нажмите **Run workflow**
5. Выберите ветку и нажмите **Run workflow**

---

## Проверка работы

### 1. Мониторинг выполнения

```
GitHub → Actions → Deploy to VPS via Private Registry → Latest run
```

Вы увидите три job'а:

- ✅ **Run Tests** - запуск тестов
- 🔨 **Build and Push to Registry** - сборка и push
- 🚀 **Deploy to VPS** - деплой

### 2. Просмотр логов

Кликните на любой job для просмотра детальных логов каждого шага.

### 3. Проверка на VPS

```bash
# Подключитесь к VPS
ssh user@vps-ip

# Проверьте статус
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps

# Проверьте логи
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f family-task-manager

# Проверьте образы в registry
curl http://localhost:5000/v2/_catalog
curl http://localhost:5000/v2/family-task-manager/tags/list
```

---

## Структура workflow

```yaml
┌─────────────────────────────────────────────────────────────┐
│  Job 1: Run Tests                                           │
│  ├── Checkout code                                          │
│  ├── Setup .NET                                             │
│  ├── Restore dependencies                                   │
│  ├── Build                                                  │
│  └── Run tests                                              │
└────────────────────────────┬────────────────────────────────┘
                             │ If tests pass
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 2: Build and Push to Registry                          │
│  ├── Checkout code                                          │
│  ├── Setup Docker Buildx                                    │
│  ├── Setup SSH tunnel to VPS registry                       │
│  ├── Login to Private Registry                              │
│  ├── Extract metadata (commit, branch, date)                │
│  └── Build and push Docker image                            │
│      ├── Tag: latest                                        │
│      ├── Tag: commit-hash                                   │
│      └── Tag: branch-name                                   │
└────────────────────────────┬────────────────────────────────┘
                             │ If push successful
                             ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 3: Deploy to VPS                                       │
│  ├── SSH to VPS                                             │
│  ├── Check registry availability                            │
│  ├── Run deploy-from-registry.sh                            │
│  ├── Verify deployment                                      │
│  └── Show logs                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Troubleshooting

### ❌ Error: Registry unavailable

**Проблема:** SSH туннель не установлен или registry не запущен

**Решение:**

```bash
# На VPS проверьте статус registry
docker compose -f /opt/docker-registry/docker-compose.registry.yml ps

# Перезапустите registry
docker compose -f /opt/docker-registry/docker-compose.registry.yml restart

# Проверьте доступность
curl http://localhost:5000/v2/_catalog
```

### ❌ Error: Permission denied (publickey)

**Проблема:** SSH ключ не добавлен на VPS или неправильный формат

**Решение:**

```bash
# Проверьте authorized_keys на VPS
cat ~/.ssh/authorized_keys

# Проверьте права доступа
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys

# Добавьте ключ заново
cat github_actions_key.pub | ssh user@vps 'cat >> ~/.ssh/authorized_keys'
```

### ❌ Error: Login failed to registry

**Проблема:** Неправильные credentials для registry

**Решение:**

```bash
# На VPS проверьте htpasswd файл
cat /opt/docker-registry/registry-auth/htpasswd

# Пересоздайте пользователя
htpasswd -Bc /opt/docker-registry/registry-auth/htpasswd deploy-user

# Обновите GitHub Secret REGISTRY_PASSWORD
```

### ❌ Error: Tests failed

**Проблема:** Тесты не проходят

**Решение:**

```bash
# Запустите тесты локально
dotnet test FamilyTaskManager.sln --configuration Release

# Проверьте логи в GitHub Actions
# Исправьте ошибки и сделайте новый commit
```

### ❌ Error: Deployment failed

**Проблема:** Приложение не запустилось на VPS

**Решение:**

```bash
# На VPS проверьте логи
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs family-task-manager

# Проверьте .env файл
cat /opt/family-task-manager/.env

# Проверьте образ
docker images localhost:5000/family-task-manager

# Попробуйте ручной деплой
cd /opt/family-task-manager
bash scripts/deploy-from-registry.sh
```

---

## Расширенная настройка

### Уведомления в Telegram

Добавьте в конец workflow:

```yaml
  notify:
    name: Send Notification
    runs-on: ubuntu-latest
    needs: deploy
    if: always()
    
    steps:
      - name: Send Telegram notification
        uses: appleboy/telegram-action@master
        with:
          to: ${{ secrets.TELEGRAM_CHAT_ID }}
          token: ${{ secrets.TELEGRAM_BOT_TOKEN }}
          message: |
            🚀 Deployment ${{ job.status }}
            
            Repository: ${{ github.repository }}
            Branch: ${{ github.ref_name }}
            Commit: ${{ github.sha }}
            Author: ${{ github.actor }}
```

Добавьте secrets:

- `TELEGRAM_BOT_TOKEN` - токен бота для уведомлений
- `TELEGRAM_CHAT_ID` - ваш chat ID

### Деплой только при наличии тега

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```

### Деплой в разные окружения

```yaml
on:
  push:
    branches:
      - main        # Production
      - develop     # Staging
```

---

## Безопасность

### ✅ Рекомендации

1. **Используйте отдельный SSH ключ** для GitHub Actions
2. **Ограничьте права пользователя** на VPS (не используйте root)
3. **Регулярно ротируйте secrets** (раз в 3-6 месяцев)
4. **Используйте сильные пароли** для registry
5. **Включите 2FA** на GitHub аккаунте
6. **Ограничьте доступ к secrets** только необходимым workflow

### ⚠️ Не делайте

1. ❌ Не коммитьте secrets в код
2. ❌ Не используйте root пользователя для деплоя
3. ❌ Не храните пароли в логах
4. ❌ Не используйте слабые пароли
5. ❌ Не давайте широкие права SSH ключу

---

## Мониторинг и логи

### GitHub Actions логи

```
GitHub → Actions → Workflow run → Job → Step
```

Логи хранятся 90 дней.

### VPS логи

```bash
# Логи деплоя
cat /opt/family-task-manager/deploy.log

# Логи приложения
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs -f

# Логи registry
docker logs docker-registry -f
```

---

## Чек-лист настройки

- [ ] Registry настроен и работает на VPS
- [ ] SSH ключ создан для GitHub Actions
- [ ] Публичный ключ добавлен на VPS
- [ ] Все GitHub Secrets созданы:
    - [ ] `VPS_HOST`
    - [ ] `VPS_USERNAME`
    - [ ] `VPS_SSH_KEY`
    - [ ] `REGISTRY_USERNAME`
    - [ ] `REGISTRY_PASSWORD`
- [ ] Workflow файл `deploy-registry.yml` существует
- [ ] Тесты проходят локально
- [ ] Первый ручной запуск workflow успешен
- [ ] Автоматический деплой работает при push

---

## Дополнительные ресурсы

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Private Registry Setup](../docs/PRIVATE_REGISTRY_SETUP.md)
- [Deployment Checklist](DEPLOYMENT_CHECKLIST.md)
- [Registry Commands Cheatsheet](../docs/REGISTRY_COMMANDS_CHEATSHEET.md)

---

**Готово!** Теперь каждый push в `main` будет автоматически деплоить приложение на VPS через Private Registry. 🚀
