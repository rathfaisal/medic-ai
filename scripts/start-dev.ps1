$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Write-Host "Starting MEDIC-AI with Docker Compose..." -ForegroundColor Cyan
Set-Location $root
if (!(Test-Path ".env")) { Copy-Item ".env.example" ".env" }
docker compose up --build -d
Write-Host "Waiting for services..." -ForegroundColor Yellow
Start-Sleep -Seconds 8
try {
    $health = Invoke-RestMethod "http://localhost:5080/health"
    Write-Host "API status: $($health.status)" -ForegroundColor Green
} catch {
    Write-Warning "API health check is not ready yet. Run: docker compose logs api"
}
Write-Host "Web: http://localhost:3000" -ForegroundColor Green
Write-Host "Swagger: http://localhost:5080/swagger" -ForegroundColor Green
