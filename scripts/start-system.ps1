# Start FamilyTaskManager System (Bot + Worker)
# PowerShell script for Windows

param(
    [switch]$SkipBuild,
    [switch]$BotOnly,
    [switch]$WorkerOnly,
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FamilyTaskManager System Startup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get root directory
$rootDir = Split-Path -Parent $PSScriptRoot
$botDir = Join-Path $rootDir "src\FamilyTaskManager.Bot"
$workerDir = Join-Path $rootDir "src\FamilyTaskManager.Worker"
$infraDir = Join-Path $rootDir "src\FamilyTaskManager.Infrastructure"

# Check if .NET is installed
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 9.0 or higher." -ForegroundColor Red
    exit 1
}

# Check PostgreSQL connection
Write-Host ""
Write-Host "Checking PostgreSQL connection..." -ForegroundColor Yellow

if ($ConnectionString -eq "") {
    # Try to get from user secrets
    Push-Location $botDir
    $secrets = dotnet user-secrets list 2>$null
    if ($LASTEXITCODE -eq 0) {
        $connectionLine = $secrets | Select-String "ConnectionStrings:DefaultConnection"
        if ($connectionLine) {
            Write-Host "✓ Found connection string in user secrets" -ForegroundColor Green
        } else {
            Write-Host "✗ Connection string not found in user secrets" -ForegroundColor Red
            Write-Host "  Run: dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'YOUR_CONNECTION_STRING'" -ForegroundColor Yellow
            exit 1
        }
    }
    Pop-Location
}

# Check Bot token
if (-not $WorkerOnly) {
    Write-Host ""
    Write-Host "Checking Telegram Bot configuration..." -ForegroundColor Yellow
    Push-Location $botDir
    $secrets = dotnet user-secrets list 2>$null
    if ($LASTEXITCODE -eq 0) {
        $tokenLine = $secrets | Select-String "Bot:BotToken"
        if ($tokenLine) {
            Write-Host "✓ Bot token configured" -ForegroundColor Green
        } else {
            Write-Host "✗ Bot token not found" -ForegroundColor Red
            Write-Host "  Run: dotnet user-secrets set 'Bot:BotToken' 'YOUR_BOT_TOKEN'" -ForegroundColor Yellow
            exit 1
        }
    }
    Pop-Location
}

# Build projects
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building projects..." -ForegroundColor Yellow
    
    Push-Location $rootDir
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "✓ Build successful" -ForegroundColor Green
    Pop-Location
}

# Apply migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
Push-Location $infraDir
dotnet ef database update --startup-project ..\FamilyTaskManager.Web 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database migrations applied" -ForegroundColor Green
} else {
    Write-Host "⚠ Could not apply migrations (database might be up to date)" -ForegroundColor Yellow
}
Pop-Location

# Start services
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Starting Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$jobs = @()

# Start Bot
if (-not $WorkerOnly) {
    Write-Host "Starting Telegram Bot..." -ForegroundColor Yellow
    $botJob = Start-Job -ScriptBlock {
        param($dir)
        Set-Location $dir
        dotnet run --no-build --configuration Release
    } -ArgumentList $botDir
    $jobs += @{Name="Bot"; Job=$botJob}
    Write-Host "✓ Bot started (Job ID: $($botJob.Id))" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

# Start Worker
if (-not $BotOnly) {
    Write-Host "Starting Quartz Worker..." -ForegroundColor Yellow
    $workerJob = Start-Job -ScriptBlock {
        param($dir)
        Set-Location $dir
        dotnet run --no-build --configuration Release
    } -ArgumentList $workerDir
    $jobs += @{Name="Worker"; Job=$workerJob}
    Write-Host "✓ Worker started (Job ID: $($workerJob.Id))" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  System Running" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services started successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Active jobs:" -ForegroundColor Yellow
foreach ($item in $jobs) {
    Write-Host "  - $($item.Name): Job ID $($item.Job.Id)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "Commands:" -ForegroundColor Yellow
Write-Host "  View logs:    Get-Job | Receive-Job -Keep" -ForegroundColor Cyan
Write-Host "  Stop all:     Get-Job | Stop-Job; Get-Job | Remove-Job" -ForegroundColor Cyan
Write-Host "  Stop Bot:     Stop-Job -Id $($jobs[0].Job.Id)" -ForegroundColor Cyan
if ($jobs.Count -gt 1) {
    Write-Host "  Stop Worker:  Stop-Job -Id $($jobs[1].Job.Id)" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "Press Ctrl+C to view logs and stop services..." -ForegroundColor Yellow
Write-Host ""

# Monitor jobs
try {
    while ($true) {
        Start-Sleep -Seconds 5
        
        # Check if any job failed
        foreach ($item in $jobs) {
            $job = $item.Job
            if ($job.State -eq "Failed") {
                Write-Host ""
                Write-Host "✗ $($item.Name) failed!" -ForegroundColor Red
                Write-Host "Error output:" -ForegroundColor Red
                Receive-Job -Job $job
                throw "$($item.Name) job failed"
            }
        }
        
        # Show periodic status
        $timestamp = Get-Date -Format "HH:mm:ss"
        Write-Host "[$timestamp] System running... (Ctrl+C to stop)" -ForegroundColor Gray -NoNewline
        Write-Host "`r" -NoNewline
    }
} catch [System.Management.Automation.PipelineStoppedException] {
    # User pressed Ctrl+C
    Write-Host ""
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Stopping Services" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
} finally {
    # Show logs
    Write-Host "Recent logs:" -ForegroundColor Yellow
    Write-Host ""
    foreach ($item in $jobs) {
        Write-Host "--- $($item.Name) ---" -ForegroundColor Cyan
        Receive-Job -Job $item.Job -Keep | Select-Object -Last 20
        Write-Host ""
    }
    
    # Stop all jobs
    Write-Host "Stopping jobs..." -ForegroundColor Yellow
    Get-Job | Stop-Job
    Get-Job | Remove-Job
    Write-Host "✓ All services stopped" -ForegroundColor Green
}

Write-Host ""
Write-Host "System shutdown complete." -ForegroundColor Green
