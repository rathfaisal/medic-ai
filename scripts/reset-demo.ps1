$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$confirm = Read-Host "This deletes the local MEDIC-AI database and generated model. Type RESET to continue"
if ($confirm -ne "RESET") { Write-Host "Cancelled."; exit }

docker compose down -v
$modelPath = Join-Path $root "src\MedicAI.Api\MachineLearning\Artifacts\triage-model.zip"
if (Test-Path $modelPath) { Remove-Item $modelPath -Force }
docker compose up --build -d
Write-Host "Demo environment reset. Seed accounts and the synthetic demonstration model will be recreated." -ForegroundColor Green
