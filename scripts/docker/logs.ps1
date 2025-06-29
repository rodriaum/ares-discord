# Script to view container logs
param(
    [string]$Service = "",
    [switch]$Follow,
    [int]$Lines = 100
)

Write-Host "📋 Viewing container logs..." -ForegroundColor Cyan

# Navigate to project root
Set-Location $PSScriptRoot\..\..

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Error: docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Show logs
if ($Service) {
    Write-Host "📋 Logs for service: $Service" -ForegroundColor Yellow
    if ($Follow) {
        docker-compose logs -f --tail=$Lines $Service
    } else {
        docker-compose logs --tail=$Lines $Service
    }
} else {
    Write-Host "📋 Logs for all services" -ForegroundColor Yellow
    if ($Follow) {
        docker-compose logs -f --tail=$Lines
    } else {
        docker-compose logs --tail=$Lines
    }
}