Write-Host "Starting Digital Mosquito..." -ForegroundColor Green

# Close any currently running instance to prevent file locks
Get-Process -Name "DigitalMosquito" -ErrorAction SilentlyContinue | Stop-Process -Force

# Resolve executable path
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { "." }
$exePath = Join-Path $scriptDir "bin\Debug\net8.0-windows\DigitalMosquito.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Building project first..." -ForegroundColor Yellow
    dotnet build $scriptDir
}

# Start the application and keep it running
$proc = Start-Process -FilePath $exePath -PassThru

Write-Host "Digital Mosquito is running (PID: $($proc.Id))!" -ForegroundColor Cyan
Write-Host "Controls:" -ForegroundColor White
Write-Host "  - Speed: Click HUD buttons in top-right or press 1, 2, 3, 4" -ForegroundColor Gray
Write-Host "  - Swat: Click the mosquito" -ForegroundColor Gray
Write-Host "  - Clean: Rub mouse over blood splatter" -ForegroundColor Gray
Write-Host "  - Exit: Click Exit on HUD or press Esc" -ForegroundColor Gray
