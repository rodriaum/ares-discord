# Script to build Docker images
param(
    [switch]$NoCache,
    [string]$Service = ""
)

Write-Host "🐳 Starting Docker images build..." -ForegroundColor Cyan

# Navigate to project root
Set-Location $PSScriptRoot\..\..

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Error: docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Build with or without cache
if ($NoCache) {
    if ($Service) {
        Write-Host "🔨 Building $Service without cache..." -ForegroundColor Yellow
        docker-compose build --no-cache $Service
    } else {
        Write-Host "🔨 Building all images without cache..." -ForegroundColor Yellow
        docker-compose build --no-cache
    }
} else {
    if ($Service) {
        Write-Host "🔨 Building $Service..." -ForegroundColor Yellow
        docker-compose build $Service
    } else {
        Write-Host "🔨 Building all images..." -ForegroundColor Yellow
        docker-compose build
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}