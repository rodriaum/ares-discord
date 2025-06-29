# Script to check container status
Write-Host "📊 Docker container status..." -ForegroundColor Cyan

# Navigate to project root
Set-Location $PSScriptRoot\..\..

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Error: docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Show status
Write-Host "📦 Containers:" -ForegroundColor Yellow
docker-compose ps

Write-Host "`n📚 Resource usage:" -ForegroundColor Yellow
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"