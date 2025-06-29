# Script to stop Docker containers
param(
    [switch]$Remove,
    [switch]$Volumes
)

Write-Host "⏸️  Stopping Docker containers..." -ForegroundColor Cyan

# Navigate to project root
Set-Location $PSScriptRoot\..\..

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Error: docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Stop containers
if ($Remove -and $Volumes) {
    Write-Host "🗑️  Stopping and removing containers and volumes..." -ForegroundColor Yellow
    docker-compose down -v
} elseif ($Remove) {
    Write-Host "🗑️  Stopping and removing containers..." -ForegroundColor Yellow
    docker-compose down
} else {
    Write-Host "⏸️  Stopping containers..." -ForegroundColor Yellow
    docker-compose stop
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Containers stopped successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Error stopping containers!" -ForegroundColor Red
    exit 1
}