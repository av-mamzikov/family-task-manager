# Миграция на Private Registry

Пошаговая инструкция по переходу со старого метода деплоя (docker save/load) на Private Registry.

## 📊 Сравнение методов

| Параметр            | Старый метод (save/load) | Новый метод (Registry)        |
|---------------------|--------------------------|-------------------------------|
| **Время деплоя**    | ~5-7 минут               | ~2-3 минуты                   |
| **Размер передачи** | ~500 MB (полный образ)   | ~50-100 MB (только изменения) |
| **Версионирование** | ❌ Нет                    | ✅ Да                          |
| **Откат**           | ⚠️ Сложно                | ✅ Легко                       |
| **Надежность**      | ⚠️ Средняя               | ✅ Высокая                     |

## 🎯 План миграции

### Фаза 1: Подготовка (30 минут)

- [ ] Настройка Private Registry на VPS
- [ ] Проверка работы registry
- [ ] Настройка GitHub Secrets

### Фаза 2: Тестирование (15 минут)

- [ ] Ручной деплой через registry
- [ ] Проверка работоспособности
- [ ] Тестирование отката

### Фаза 3: Переключение (5 минут)

- [ ] Активация нового workflow
- [ ] Отключение старого workflow
- [ ] Первый автоматический деплой

---

## Шаг 1: Настройка Registry на VPS

### 1.1. Подключитесь к VPS

```bash
ssh user@vps-ip
```

### 1.2. Создайте директорию для registry

```bash
sudo mkdir -p /opt/docker-registry
sudo chown $USER:$USER /opt/docker-registry
cd /opt/docker-registry
```

### 1.3. Скопируйте файлы с локальной машины

```bash
# На локальной машине
cd C:\Users\avmam\source\family-tak-manager\family-tak-manager

scp docker-compose.registry.yml user@vps-ip:/opt/docker-registry/
scp scripts/setup-registry.sh user@vps-ip:/opt/docker-registry/
```

### 1.4. Запустите настройку на VPS

```bash
# На VPS
cd /opt/docker-registry
bash setup-registry.sh
```

**Важно:** Запомните username и password для registry!

### 1.5. Проверьте работу registry

```bash
# Статус
docker compose -f docker-compose.registry.yml ps

# API
curl http://localhost:5000/v2/_catalog
# Должен вернуть: {"repositories":[]}

# UI (в браузере)
http://vps-ip:5001
```

---

## Шаг 2: Обновление конфигурации приложения

### 2.1. Обновите docker-compose.prod.yml на VPS

```bash
# На VPS
cd /opt/family-task-manager

# Создайте бэкап
cp docker-compose.prod.yml docker-compose.prod.yml.backup

# Обновите образ в docker-compose.prod.yml
nano docker-compose.prod.yml
```

Измените строку:

```yaml
# Было:
image: ${DOCKER_USERNAME}/family-task-manager:latest

# Стало:
image: ${REGISTRY_HOST:-localhost:5000}/family-task-manager:latest
```

### 2.2. Обновите .env файл

```bash
# На VPS
nano /opt/family-task-manager/.env
```

Добавьте:

```env
REGISTRY_HOST=localhost:5000
```

### 2.3. Скопируйте новый скрипт деплоя

```bash
# На локальной машине
scp scripts/deploy-from-registry.sh user@vps-ip:/opt/family-task-manager/scripts/
```

---

## Шаг 3: Тестовый деплой

### 3.1. Создайте SSH туннель (на локальной машине)

```powershell
# Windows PowerShell
ssh -L 5000:localhost:5000 -N user@vps-ip
# Оставьте это окно открытым
```

### 3.2. Войдите в registry

```powershell
# В новом окне PowerShell
docker login localhost:5000
# Введите username и password из шага 1.4
```

### 3.3. Соберите и отправьте образ

```powershell
cd C:\Users\avmam\source\family-tak-manager\family-tak-manager
.\scripts\build-and-push.ps1
```

### 3.4. Проверьте образ в registry

```bash
# На VPS
curl http://localhost:5000/v2/_catalog
# Должен показать: {"repositories":["family-task-manager"]}

curl http://localhost:5000/v2/family-task-manager/tags/list
# Должен показать теги: latest, commit-hash, branch
```

### 3.5. Выполните деплой

```bash
# На VPS
cd /opt/family-task-manager
bash scripts/deploy-from-registry.sh
```

### 3.6. Проверьте работу приложения

```bash
# Статус
docker compose -f docker-compose.prod.yml ps

# Логи
docker compose -f docker-compose.prod.yml logs -f family-task-manager

# Проверьте Telegram бота
# Отправьте /start боту
```

---

## Шаг 4: Настройка GitHub Actions

### 4.1. Создайте SSH ключ для GitHub Actions

```powershell
# На локальной машине
ssh-keygen -t ed25519 -C "github-actions-deploy" -f $HOME\.ssh\github_actions_key -N '""'
```

### 4.2. Добавьте публичный ключ на VPS

```powershell
# Скопируйте публичный ключ
Get-Content $HOME\.ssh\github_actions_key.pub | ssh user@vps-ip 'cat >> ~/.ssh/authorized_keys'

# Проверьте подключение
ssh -i $HOME\.ssh\github_actions_key user@vps-ip 'echo "Connection successful!"'
```

### 4.3. Добавьте GitHub Secrets

Перейдите: `GitHub Repository → Settings → Secrets and variables → Actions`

Создайте следующие secrets:

| Name                | Value              | Где взять                                   |
|---------------------|--------------------|---------------------------------------------|
| `VPS_HOST`          | IP адрес VPS       | `curl ifconfig.me` на VPS                   |
| `VPS_USERNAME`      | SSH пользователь   | Ваш username на VPS                         |
| `VPS_SSH_KEY`       | Приватный SSH ключ | `Get-Content $HOME\.ssh\github_actions_key` |
| `REGISTRY_USERNAME` | Registry user      | Из шага 1.4                                 |
| `REGISTRY_PASSWORD` | Registry password  | Из шага 1.4                                 |

**Для VPS_SSH_KEY:**

```powershell
Get-Content $HOME\.ssh\github_actions_key
# Скопируйте весь вывод, включая:
# -----BEGIN OPENSSH PRIVATE KEY-----
# ...
# -----END OPENSSH PRIVATE KEY-----
```

---

## Шаг 5: Активация нового workflow

### 5.1. Проверьте наличие файлов

```bash
# На локальной машине
ls .github/workflows/
# Должны быть:
# - deploy.yml (старый, отключен)
# - deploy-registry.yml (новый)
```

### 5.2. Сделайте commit и push

```bash
git add .
git commit -m "Migrate to Private Registry deployment"
git push origin main
```

### 5.3. Мониторинг деплоя

1. Откройте GitHub → Actions
2. Найдите workflow "Deploy to VPS via Private Registry"
3. Следите за выполнением

**Ожидаемые этапы:**

- ✅ Run Tests (~2 мин)
- ✅ Build and Push to Registry (~3 мин)
- ✅ Deploy to VPS (~1 мин)

### 5.4. Проверка результата

```bash
# На VPS
docker compose -f /opt/family-task-manager/docker-compose.prod.yml ps
docker compose -f /opt/family-task-manager/docker-compose.prod.yml logs --tail=50
```

---

## Шаг 6: Очистка старых файлов

### 6.1. Удалите старые образы

```bash
# На VPS
docker images | grep family-task-manager
docker rmi <old-image-ids>
```

### 6.2. Удалите старые tar файлы

```bash
# На VPS
rm -f /opt/family-task-manager/*.tar.gz
```

### 6.3. Обновите старый скрипт деплоя (опционально)

```bash
# На VPS
mv /opt/family-task-manager/scripts/deploy.sh /opt/family-task-manager/scripts/deploy-legacy.sh
```

---

## Откат в случае проблем

### Если что-то пошло не так

#### Вариант 1: Откат через старый workflow

```bash
# На GitHub
# Actions → Deploy to VPS (Legacy) → Run workflow
```

#### Вариант 2: Ручной откат

```bash
# На VPS
cd /opt/family-task-manager

# Восстановите старый docker-compose
cp docker-compose.prod.yml.backup docker-compose.prod.yml

# Восстановите старый .env
nano .env
# Удалите строку REGISTRY_HOST=localhost:5000
# Верните DOCKER_USERNAME=your-dockerhub-username

# Перезапустите
docker compose down
docker compose up -d
```

---

## Проверка успешной миграции

### ✅ Чек-лист

- [ ] Registry работает на VPS
- [ ] Образ успешно push'ится в registry
- [ ] Деплой из registry работает
- [ ] GitHub Actions успешно выполняется
- [ ] Приложение работает после деплоя
- [ ] Telegram бот отвечает
- [ ] Логи не содержат ошибок
- [ ] Старый workflow отключен

### 📊 Метрики улучшения

Сравните время деплоя:

**До миграции:**

```
Build → Save → SCP → Load → Deploy
2 мин + 1 мин + 2 мин + 1 мин + 1 мин = 7 минут
```

**После миграции:**

```
Build → Push → Pull → Deploy
2 мин + 30 сек + 20 сек + 30 сек = 3 минуты 20 секунд
```

**Ускорение: ~2x** 🚀

---

## Troubleshooting

### Проблема: Registry недоступен из GitHub Actions

**Решение:**

```bash
# На VPS проверьте статус
docker compose -f /opt/docker-registry/docker-compose.registry.yml ps

# Проверьте firewall
sudo ufw status

# Убедитесь, что SSH работает
ssh user@vps-ip 'curl http://localhost:5000/v2/_catalog'
```

### Проблема: Ошибка аутентификации в registry

**Решение:**

```bash
# На VPS пересоздайте пользователя
cd /opt/docker-registry
htpasswd -Bc registry-auth/htpasswd deploy-user

# Обновите GitHub Secret REGISTRY_PASSWORD
```

### Проблема: Образ не pull'ится на VPS

**Решение:**

```bash
# На VPS проверьте образ в registry
curl http://localhost:5000/v2/family-task-manager/tags/list

# Попробуйте pull вручную
docker pull localhost:5000/family-task-manager:latest

# Проверьте логи registry
docker logs docker-registry
```

---

## Следующие шаги

После успешной миграции:

1. **Настройте мониторинг**
    - Добавьте уведомления в Telegram
    - Настройте алерты при ошибках деплоя

2. **Оптимизируйте процесс**
    - Настройте кеширование слоев Docker
    - Добавьте параллельное выполнение тестов

3. **Документируйте**
    - Обновите README с новым процессом
    - Обучите команду новому workflow

4. **Автоматизируйте**
    - Настройте автоматические бэкапы
    - Добавьте автоматическую очистку старых образов

---

## Полезные ссылки

- [GitHub Actions Setup](GITHUB_ACTIONS_SETUP.md)
- [Private Registry Setup](../docs/PRIVATE_REGISTRY_SETUP.md)
- [Registry Commands Cheatsheet](../docs/REGISTRY_COMMANDS_CHEATSHEET.md)
- [Deployment Checklist](DEPLOYMENT_CHECKLIST.md)

---

**Поздравляем с успешной миграцией!** 🎉

Теперь ваш процесс деплоя стал быстрее, надежнее и удобнее.
