param(
    [ValidateSet("Standalone", "Lightweight", "Both")]
    [string]$Type = "Standalone",

    [switch]$CreateZip = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  Pandaara കൊതുക് - Release Publisher   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Ensure project file exists
$projectPath = Join-Path $PSScriptRoot "DigitalMosquito.csproj"
if (-not (Test-Path $projectPath)) {
    $projectPath = Join-Path $PSScriptRoot "DigitalMosquito\DigitalMosquito.csproj"
}
if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found at: $projectPath"
}

# 2. Stop any running instances to avoid file locks
Write-Host "`nChecking for running instances..." -ForegroundColor Yellow
Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like "*Mosquito*" -or $_.ProcessName -like "*Pandaara*" } | Stop-Process -Force
Start-Sleep -Milliseconds 500

$publishRootDir = Join-Path $PSScriptRoot "publish"

# Helper function to package into zip
function Package-Release($sourceDir, $zipName) {
    $zipPath = Join-Path $publishRootDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    # Add quick-start README.txt to the package
    $readmeContent = @"
Pandaara കൊതുക് (Digital Mosquito) & Desktop Creatures
======================================================
How to run:
  Double-click 'DigitalMosquito.exe' to start!

Features & Progression:
  - Total-Kill Weapon Progression (11 Tiers):
      Lv. 1 (0 Kills)   : 👋 Hand
      Lv. 2 (5 Kills)   : 🪰 Fly Swatter
      Lv. 3 (10 Kills)  : 📰 Newspaper
      Lv. 4 (15 Kills)  : 🩴 Slipper
      Lv. 5 (25 Kills)  : 🏏 Bat
      Lv. 6 (40 Kills)  : 🔨 Hammer
      Lv. 7 (60 Kills)  : 🌀 Vacuum
      Lv. 8 (80 Kills)  : 🌊 Water Wiper (Washer)
      Lv. 9 (100 Kills) : ⚡ Electric Swatter
      Lv. 10 (150 Kills): 🩴 Giant Slipper
      Lv. 11 (250 Kills): 👑 Boss Mode

  - Upgradeable Cleaner Tools (5 Tiers):
      Tier 1 (0 Kills)  : 🧻 Tissue Paper (R: 38px, Power: 0.28, +10 XP)
      Tier 2 (10 Kills) : 🧽 Microfiber Sponge (R: 55px, Power: 0.45, +15 XP, Soap Bubbles)
      Tier 3 (25 Kills) : 🪟 Glass Squeegee (R: 78px, Power: 0.65, +25 XP, Squeak Audio)
      Tier 4 (80 Kills) : 🌀 Turbo Hydro-Scrubber (R: 110px, Power: 0.95, +40 XP, Torrents)
      Tier 5 (250 Kills): ⚡ Sonic Laser Sanitizer (R: 160px, Power: 1.40, +75 XP, Gleam)

Controls:
  - Swat / Attack  : Left-click creature with equipped weapon
  - Switch Weapon  : Click bottom progress bar to cycle unlocked weapons
  - Clean Blood    : Hold left-click and rub cursor over blood splatters
  - Pause / Resume : Click '⏸' on HUD or press 'P' / Space
  - Toggle Audio   : Click '🔊' / '🔇' on HUD or press 'M'
  - Reset Progress : Click '🔄' on HUD or press 'R' (click again to confirm reset)
  - Settings (⚙)   : Click '⚙' on HUD for Creature Types, Speed, Jumpscares & Respawn
  - Cycle Creature : Click creature icon on HUD or press 'C'
  - Collapse HUD   : Click '◀' to collapse to mini icon [ 🦟 ] (click to re-open)
  - Speed Presets  : Keys 1 (0.5x), 2 (1.0x), 3 (1.8x), 4 (2.8x)
  - Exit           : Click '×' on HUD or press Escape
"@
    [System.IO.File]::WriteAllText((Join-Path $sourceDir "README.txt"), $readmeContent, [System.Text.Encoding]::UTF8)

    Write-Host "Creating ZIP archive: $zipName..." -ForegroundColor Yellow
    Compress-Archive -Path "$sourceDir\*" -DestinationPath $zipPath -Force
    Write-Host "ZIP ready at: $zipPath" -ForegroundColor Green
}

# 3. Build Standalone (Zero dependencies)
if ($Type -eq "Standalone" -or $Type -eq "Both") {
    $outDir = Join-Path $publishRootDir "DigitalMosquito-Standalone-win-x64"
    Write-Host "`n[1/2] Building Standalone Portable Executable (win-x64)..." -ForegroundColor Magenta
    Write-Host "  -> Output: $outDir" -ForegroundColor Gray
    
    dotnet publish $projectPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $outDir

    if ($CreateZip) {
        Package-Release $outDir "DigitalMosquito-Standalone-win-x64.zip"
    }
}

# 4. Build Lightweight (Framework-dependent)
if ($Type -eq "Lightweight" -or $Type -eq "Both") {
    $outDir = Join-Path $publishRootDir "DigitalMosquito-Lightweight-win-x64"
    Write-Host "`n[2/2] Building Lightweight Executable (Requires .NET 8 Desktop Runtime)..." -ForegroundColor Magenta
    Write-Host "  -> Output: $outDir" -ForegroundColor Gray
    
    dotnet publish $projectPath `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $outDir

    if ($CreateZip) {
        Package-Release $outDir "DigitalMosquito-Lightweight-win-x64.zip"
    }
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "  Build & Publish Completed Successfully! " -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Published files are located in:" -ForegroundColor White
Write-Host "  $publishRootDir" -ForegroundColor Cyan
Write-Host "`nShare the 'DigitalMosquito-Standalone-win-x64.zip' with anyone on Windows 10/11 - no installation needed!`n" -ForegroundColor Yellow
