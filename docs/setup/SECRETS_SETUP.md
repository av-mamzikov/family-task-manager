# 🔐 Настройка GitHub Secrets

Быстрая настройка секретов для автоматического деплоя через GitHub Actions.

## ⚡ Быстрый старт (5 минут)

### Где добавлять

```
GitHub → Ваш репозиторий → Settings → Secrets and variables → Actions → New repository secret
```

### Обязательные секреты (10 штук)

| Имя секрета             | Что вставить          | Пример                                   |
|-------------------------|-----------------------|------------------------------------------|
| `VPS_HOST`              | IP адрес VPS          | `123.45.67.89`                           |
| `VPS_USERNAME`          | SSH username          | `deploy`                                 |
| `VPS_SSH_KEY`           | Приватный SSH ключ    | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `REGISTRY_USERNAME`     | Username registry     | `admin`                                  |
| `REGISTRY_PASSWORD`     | Пароль registry       | `SecurePass123!`                         |
| `TELEGRAM_BOT_TOKEN`    | Токен бота            | `1234567890:ABCdef...`                   |
| `TELEGRAM_BOT_USERNAME` | Username бота (БЕЗ @) | `MyFamilyBot`                            |
| `CHAT_URL`              | URL чата для идей     | `https://t.me/your_feedback_chat`        |
| `POSTGRES_USER`         | PostgreSQL user       | `familytask`                             |
| `POSTGRES_PASSWORD`     | PostgreSQL пароль     | `SuperSecure123!`                        |

### Опциональные для PR Preview (5 штук)

| Имя секрета            | Что вставить                |
|------------------------|-----------------------------|
| `PR_BOT_TOKEN`         | Токен тестового бота        |
| `PR_BOT_USERNAME`      | Username тестового бота     |
| `PR_CHAT_URL`          | URL чата для тестовой среды |
| `PR_POSTGRES_USER`     | `familytask_pr`             |
| `PR_POSTGRES_PASSWORD` | Пароль для тестовой БД      |

---

## 📋 Подробные инструкции

### 1. VPS и SSH подключение

#### `VPS_HOST`

- **Описание:** IP адрес вашего VPS сервера
- **Как получить:** IP адрес из панели управления хостинга

#### `VPS_USERNAME`

- **Описание:** SSH username для подключения к VPS
- **По умолчанию:** `deploy` (если использовали `init-vps.sh`)

#### `VPS_SSH_KEY`

- **Описание:** Приватный SSH ключ для подключения к VPS
- **Как получить:**
  ```bash
  # Windows
  Get-Content $HOME\.ssh\github_actions_key
  
  # Linux/Mac
  cat ~/.ssh/github_actions_key
  ```
- **⚠️ Важно:** Это ПРИВАТНЫЙ ключ! Публичный ключ должен быть на VPS в `~/.ssh/authorized_keys`

### 2. Docker Registry

#### `REGISTRY_USERNAME`

- **Описание:** Username для Docker Registry
- **Как получить:** Вы создали его при настройке VPS через `init-vps.sh`

#### `REGISTRY_PASSWORD`

- **Описание:** Пароль для Docker Registry
- **⚠️ Важно:** Используйте сильный пароль (16+ символов)

### 3. Production окружение

#### `TELEGRAM_BOT_TOKEN`

- **Описание:** Токен production Telegram бота
- **Как получить:**
    1. Откройте [@BotFather](https://t.me/BotFather) в Telegram
    2. Отправьте `/newbot`
    3. Следуйте инструкциям
    4. Скопируйте токен

> 📖 Подробнее: [Telegram Bot Setup](../TELEGRAM_BOT_SETUP.md)

#### `TELEGRAM_BOT_USERNAME`

- **Описание:** Username production бота (БЕЗ символа @)
- **Как получить:** Username, который вы указали при создании бота

#### `CHAT_URL`

- **Описание:** URL чата или канала для сбора идей и предложений от пользователей
- **Формат:** Telegram ссылка (например, `https://t.me/your_feedback_chat`)
- **Использование:** Отображается в Help-сообщении бота как ссылка "Оставить идею или предложение"
- **Опционально:** Если не указать, ссылка не будет отображаться

#### `POSTGRES_USER`

- **Описание:** PostgreSQL username для production БД
- **Рекомендация:** Используйте осмысленное имя, не `postgres`

#### `POSTGRES_PASSWORD`

- **Описание:** Пароль для PostgreSQL production БД
- **⚠️ Важно:** Используйте сильный пароль (минимум 16 символов)
- **Генерация:**
  ```bash
  # Linux/Mac
  openssl rand -base64 32
  
  # PowerShell
  -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
  ```

---

## 🚀 Быстрые команды

### Получить SSH ключ

```bash
# Linux/Mac
cat ~/.ssh/github_actions_key

# Windows PowerShell
Get-Content $env:USERPROFILE\.ssh\github_actions_key
```

### Сгенерировать пароль

```bash
# Linux/Mac
openssl rand -base64 32

# PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
```

---

## ✅ Проверка

После добавления всех секретов вы должны увидеть:

```
✅ VPS_HOST
✅ VPS_USERNAME
✅ VPS_SSH_KEY
✅ REGISTRY_USERNAME
✅ REGISTRY_PASSWORD
✅ TELEGRAM_BOT_TOKEN
✅ TELEGRAM_BOT_USERNAME
✅ CHAT_URL
✅ POSTGRES_USER
✅ POSTGRES_PASSWORD
```

**Опционально (для PR Preview):**

```
✅ PR_BOT_TOKEN
✅ PR_BOT_USERNAME
✅ PR_CHAT_URL
✅ PR_POSTGRES_USER
✅ PR_POSTGRES_PASSWORD
```

---

## 🔐 Безопасность

### ✅ Хорошие практики

- **Никогда не коммитьте секреты в код**
- **Используйте сильные пароли** (минимум 16 символов)
- **Регулярно обновляйте секреты** (каждые 3-6 месяцев)
- **Используйте разные пароли** для разных окружений

### ❌ Плохие практики

- ❌ Не используйте простые пароли (`password123`)
- ❌ Не используйте одинаковые пароли для production и testing
- ❌ Не храните секреты в `.env` файлах в репозитории

---

## 🎬 Тестовый запуск

После настройки секретов:

1. GitHub → Actions
2. Deploy to VPS → Run workflow
3. Выберите ветку → Run workflow
4. Следите за логами

---

## 🔧 Troubleshooting

### Ошибка: "Secret not found"

**Решение:**

1. Проверьте, что имя секрета написано ЗАГЛАВНЫМИ буквами
2. Проверьте, что секрет добавлен в правильный репозиторий

### Ошибка: "Permission denied (publickey)"

**Решение:**

1. Убедитесь, что вы добавили ПРИВАТНЫЙ ключ в `VPS_SSH_KEY`
2. Убедитесь, что ПУБЛИЧНЫЙ ключ добавлен на VPS:
   ```bash
   ssh deploy@ваш_ip
   cat ~/.ssh/authorized_keys
   ```

### Ошибка: "Registry authentication failed"

**Решение:**

1. Проверьте `REGISTRY_USERNAME` и `REGISTRY_PASSWORD`
2. Проверьте, что registry работает на VPS:
   ```bash
   ssh deploy@ваш_ip
   docker ps | grep registry
   ```

### Ошибка: "Bot token is invalid"

**Решение:**

1. Проверьте токен у @BotFather: отправьте `/token`
2. Убедитесь, что скопировали токен полностью
3. Проверьте, что нет лишних пробелов

---

## 📚 Дополнительные ресурсы

- [GitHub Actions Setup](GITHUB_ACTIONS_SETUP.md) - настройка CI/CD
- [VPS Setup](VPS_SETUP.md) - настройка сервера
- [Telegram Bot Setup](../TELEGRAM_BOT_SETUP.md) - создание бота

---

**Время настройки:** ~10 минут
