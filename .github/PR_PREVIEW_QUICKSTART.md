# 🚀 PR Preview - Быстрый старт

Краткая инструкция по настройке и использованию PR Preview окружений.

## ⚡ За 5 минут

### 1. Создайте тестовый Telegram бот

```
1. Откройте @BotFather в Telegram
2. /newbot
3. Имя: "MyApp PR Preview Bot"
4. Username: your_app_pr_bot
5. Сохраните токен
```

### 2. Добавьте GitHub Secrets

`GitHub → Settings → Secrets and variables → Actions → New repository secret`

| Secret                 | Value             |
|------------------------|-------------------|
| `PR_BOT_TOKEN`         | Токен из шага 1   |
| `PR_BOT_USERNAME`      | `your_app_pr_bot` |
| `PR_POSTGRES_USER`     | `familytask_pr`   |
| `PR_POSTGRES_PASSWORD` | Придумайте пароль |

### 3. Используйте!

```bash
# Создайте PR
git checkout -b feature/my-feature
git push origin feature/my-feature

# На GitHub:
# 1. Создайте PR
# 2. Перейдите в Actions → Deploy to VPS via Private Registry
# 3. Нажмите "Run workflow"
#    - Branch: выберите вашу ветку
#    - Environment: pr-preview
#    - PR number: введите номер PR
# 4. Дождитесь деплоя (~5 мин)
# 5. Тестируйте в Telegram @your_app_pr_bot
```

---

## 📝 Полная документация

См. [PR_PREVIEW_ENVIRONMENTS.md](PR_PREVIEW_ENVIRONMENTS.md)

---

## 🎯 Как это работает

```
PR создан → Кнопка "Run workflow" → Tests → Build → Push → Deploy → Комментарий в PR
                                      ✅      🔨     📤     🚀      💬

Изолированное окружение:
- Отдельная БД: FamilyTaskManager_PR_{number}
- Отдельный бот: @your_app_pr_bot
- Уникальные имена контейнеров
- Тот же docker-compose.prod.yml с переопределенными параметрами
```

---

## ✅ Чек-лист

- [ ] Тестовый бот создан
- [ ] 4 GitHub Secrets добавлены
- [ ] Workflow файлы на месте:
    - [ ] `.github/workflows/deploy-registry.yml` (универсальный)
    - [ ] `.github/workflows/cleanup-pr-preview.yml`
- [ ] VPS имеет достаточно ресурсов (4+ GB RAM)

---

## 🆘 Проблемы?

### Workflow не запускается

```bash
# Убедитесь что запускаете вручную
GitHub → Actions → Deploy to VPS via Private Registry → Run workflow
# Выберите pr-preview и укажите номер PR
```

### Бот не отвечает

```bash
# На VPS проверьте логи
ssh user@vps
docker logs family-task-manager-pr-{NUMBER} -f
```

### Недостаточно ресурсов

```bash
# Очистите старые preview
cd /opt
ls -d family-task-manager-pr-*
# Удалите ненужные
```

---

**Готово!** Теперь вы можете тестировать PR перед слиянием в main. 🎉
