# Script to start Docker containers
param(
    [switch]$Build,
    [switch]$Detached = $true
)

Write-Host "🚀 Starting Docker containers..." -ForegroundColor Cyan

# Navigate to project root
Set-Location $PSScriptRoot\..\..

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Error: docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Check if .env exists
if (-not (Test-Path ".env")) {
    Write-Host "⚠️  Warning: .env file not found!" -ForegroundColor Yellow
    Write-Host "   Creating from .env.example..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env" -ErrorAction SilentlyContinue
}

# Start containers
if ($Build) {
    Write-Host "🔨 Building and starting containers..." -ForegroundColor Yellow
    if ($Detached) {
        docker-compose up -d --build
    } else {
        docker-compose up --build
    }
} else {
    Write-Host "▶️  Starting containers..." -ForegroundColor Yellow
    if ($Detached) {
        docker-compose up -d
    } else {
        docker-compose up
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Containers started successfully!" -ForegroundColor Green
    Write-Host "📊 Container status:" -ForegroundColor Cyan
    docker-compose ps
} else {
    Write-Host "❌ Error starting containers!" -ForegroundColor Red
    exit 1
}