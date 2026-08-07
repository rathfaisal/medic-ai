$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Write-Host "This script expects PostgreSQL on localhost:5432." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/MedicAI.Api'; dotnet restore; dotnet run"
Start-Sleep -Seconds 3
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root/src/medic-ai-web'; npm install; npm run dev"
Write-Host "API: http://localhost:5080 | Web: http://localhost:5173" -ForegroundColor Green
