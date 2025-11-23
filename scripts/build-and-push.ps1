# PowerShell версия скрипта для сборки и отправки образа в Private Registry
# Запускать локально на Windows: .\build-and-push.ps1

param(
    [string]$RegistryHost = "localhost:5000",
    [string]$RegistryUser = "",
    [string]$RegistryPassword = ""
)

$ErrorActionPreference = "Stop"

# Конфигурация
$ImageName = "family-task-manager"
$FullImageName = "$RegistryHost/$ImageName"

# Получение версии из Git
$GitCommit = (git rev-parse --short HEAD).Trim()
$GitBranch = (git rev-parse --abbrev-ref HEAD).Trim()
$BuildDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

Write-Host "=== Сборка и отправка образа в Private Registry ===" -ForegroundColor Cyan
Write-Host "Registry:  $RegistryHost"
Write-Host "Image:     $ImageName"
Write-Host "Commit:    $GitCommit"
Write-Host "Branch:    $GitBranch"
Write-Host ""

# Проверка доступности registry
Write-Host "Проверка доступности registry..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://$RegistryHost/v2/_catalog" -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ Registry доступен" -ForegroundColor Green
} catch {
    Write-Host "✗ Ошибка: Registry недоступен по адресу $RegistryHost" -ForegroundColor Red
    Write-Host "Убедитесь, что:" -ForegroundColor Yellow
    Write-Host "  1. Registry запущен на VPS"
    Write-Host "  2. Вы подключили SSH tunnel:"
    Write-Host "     ssh -L 5000:localhost:5000 user@vps-ip"
    exit 1
}

# Вход в registry (если требуется)
if ($RegistryUser -and $RegistryPassword) {
    Write-Host ""
    Write-Host "Вход в registry..." -ForegroundColor Yellow
    $RegistryPassword | docker login $RegistryHost -u $RegistryUser --password-stdin
}

# Сборка образа
Write-Host ""
Write-Host "Сборка Docker образа..." -ForegroundColor Yellow
docker build `
    --build-arg BUILD_CONFIGURATION=Release `
    --label "git.commit=$GitCommit" `
    --label "git.branch=$GitBranch" `
    --label "build.date=$BuildDate" `
    --tag "$FullImageName:latest" `
    --tag "$FullImageName:$GitCommit" `
    --tag "$FullImageName:$GitBranch" `
    .

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Ошибка при сборке образа" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Образ собран успешно" -ForegroundColor Green

# Отправка образа в registry
Write-Host ""
Write-Host "Отправка образа в registry..." -ForegroundColor Yellow
docker push "$FullImageName:latest"
docker push "$FullImageName:$GitCommit"
docker push "$FullImageName:$GitBranch"

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Ошибка при отправке образа" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Образ успешно отправлен в registry!" -ForegroundColor Green
Write-Host ""
Write-Host "Доступные теги:" -ForegroundColor Cyan
Write-Host "  - $FullImageName:latest"
Write-Host "  - $FullImageName:$GitCommit"
Write-Host "  - $FullImageName:$GitBranch"
Write-Host ""
Write-Host "Для деплоя на VPS выполните:" -ForegroundColor Yellow
Write-Host "  ssh user@vps-ip 'cd /opt/family-task-manager && bash scripts/deploy-from-registry.sh'"
