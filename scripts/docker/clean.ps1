# Script to clean unused Docker resources
param(
    [switch]$All,
    [switch]$Images,
    [switch]$Volumes
)

Write-Host "🧹 Cleaning Docker resources..." -ForegroundColor Cyan

if ($All) {
    Write-Host "🗑️  Cleaning all unused resources..." -ForegroundColor Yellow
    docker system prune -a -f --volumes
} elseif ($Images) {
    Write-Host "🖼️  Cleaning unused images..." -ForegroundColor Yellow
    docker image prune -a -f
} elseif ($Volumes) {
    Write-Host "💾 Cleaning unused volumes..." -ForegroundColor Yellow
    docker volume prune -f
} else {
    Write-Host "🗑️  Cleaning basic resources..." -ForegroundColor Yellow
    docker system prune -f
}

Write-Host "✅ Cleanup completed!" -ForegroundColor Green