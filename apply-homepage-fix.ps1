param(
  [string]$ProjectRoot = "D:\UNISEL\Prem\FYP\MEDIC-AI-Full-System"
)

$ErrorActionPreference = "Stop"
$bundleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$files = @(
  "src\medic-ai-web\src\App.jsx",
  "src\medic-ai-web\src\pages\HomePage.jsx",
  "src\medic-ai-web\src\pages\LoginPage.jsx",
  "src\medic-ai-web\src\pages\RegisterPage.jsx",
  "src\medic-ai-web\src\components\ProtectedRoute.jsx",
  "src\medic-ai-web\src\components\Layout.jsx",
  "src\medic-ai-web\src\styles\main.css",
  "src\MedicAI.Api\Contracts\Contracts.cs"
)

foreach ($relative in $files) {
  $source = Join-Path $bundleRoot $relative
  $destination = Join-Path $ProjectRoot $relative
  $destinationDir = Split-Path -Parent $destination

  if (-not (Test-Path $source)) {
    throw "Missing bundle file: $source"
  }

  New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
  Copy-Item -Force $source $destination
  Write-Host "Updated: $relative" -ForegroundColor Green
}

Write-Host ""
Write-Host "MEDIC-AI homepage/login fix applied successfully." -ForegroundColor Cyan
Write-Host "Now run:"
Write-Host "  cd `"$ProjectRoot`""
Write-Host "  git add ."
Write-Host "  git commit -m `"Add public homepage and fix record validation`""
Write-Host "  git push"
