# ⚡ Быстрая установка GitHub Secrets

Краткая шпаргалка для быстрой настройки секретов.

## 🎯 Где добавлять

```
GitHub → Ваш репозиторий → Settings → Secrets and variables → Actions → New repository secret
```

## 📊 Визуальная схема

```
┌─────────────────────────────────────────────────────────────┐
│  GitHub Repository                                          │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Settings ⚙️                                          │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  Secrets and variables                          │  │  │
│  │  │  ┌───────────────────────────────────────────┐  │  │  │
│  │  │  │  Actions                                  │  │  │  │
│  │  │  │  ┌─────────────────────────────────────┐  │  │  │  │
│  │  │  │  │  New repository secret              │  │  │  │  │
│  │  │  │  │                                     │  │  │  │  │
│  │  │  │  │  Name:  VPS_HOST                    │  │  │  │  │
│  │  │  │  │  Value: 123.45.67.89                │  │  │  │  │
│  │  │  │  │                                     │  │  │  │  │
│  │  │  │  │  [Add secret]                       │  │  │  │  │
│  │  │  │  └─────────────────────────────────────┘  │  │  │  │
│  │  │  └───────────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 📋 Список секретов для копирования

### Обязательные (9 секретов)

| Имя секрета             | Что вставить          | Пример                                   |
|-------------------------|-----------------------|------------------------------------------|
| `VPS_HOST`              | IP адрес VPS          | `123.45.67.89`                           |
| `VPS_USERNAME`          | SSH username          | `root`                                   |
| `VPS_SSH_KEY`           | Приватный SSH ключ    | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `REGISTRY_USERNAME`     | Username registry     | `admin`                                  |
| `REGISTRY_PASSWORD`     | Пароль registry       | `SecurePass123!`                         |
| `TELEGRAM_BOT_TOKEN`    | Токен бота            | `1234567890:ABCdef...`                   |
| `TELEGRAM_BOT_USERNAME` | Username бота (БЕЗ @) | `MyFamilyBot`                            |
| `POSTGRES_USER`         | PostgreSQL user       | `familytask`                             |
| `POSTGRES_PASSWORD`     | PostgreSQL пароль     | `SuperSecure123!`                        |

### Опциональные для PR Preview (4 секрета)

| Имя секрета            | Что вставить            |
|------------------------|-------------------------|
| `PR_BOT_TOKEN`         | Токен тестового бота    |
| `PR_BOT_USERNAME`      | Username тестового бота |
| `PR_POSTGRES_USER`     | `familytask_pr`         |
| `PR_POSTGRES_PASSWORD` | Пароль для тестовой БД  |

## 🚀 Быстрые команды

### Получить SSH ключ

```bash
# Linux/Mac
cat ~/.ssh/id_rsa

# Windows PowerShell
Get-Content $env:USERPROFILE\.ssh\id_rsa
```

### Сгенерировать сильный пароль

```bash
# Linux/Mac
openssl rand -base64 32

# PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
```

### Создать Telegram бота

1. Откройте [@BotFather](https://t.me/BotFather)
2. Отправьте `/newbot`
3. Следуйте инструкциям
4. Скопируйте токен

## ✅ Проверка

После добавления всех секретов:

```
✅ VPS_HOST
✅ VPS_USERNAME
✅ VPS_SSH_KEY
✅ REGISTRY_USERNAME
✅ REGISTRY_PASSWORD
✅ TELEGRAM_BOT_TOKEN
✅ TELEGRAM_BOT_USERNAME
✅ POSTGRES_USER
✅ POSTGRES_PASSWORD
```

## 🎬 Тестовый запуск

1. GitHub → Actions
2. Deploy to VPS → Run workflow
3. Выберите ветку → Run workflow
4. Следите за логами

## ⚠️ Важно

- ❌ **НЕ** коммитьте секреты в код
- ✅ Используйте **сильные пароли** (16+ символов)
- ✅ **Разные пароли** для production и testing
- ✅ Добавляйте **ПРИВАТНЫЙ** SSH ключ, не публичный

## 📖 Подробная инструкция

Полная документация: [GitHub Secrets Setup](GITHUB_SECRETS_SETUP.md)

---

**Время настройки:** ~10 минут
