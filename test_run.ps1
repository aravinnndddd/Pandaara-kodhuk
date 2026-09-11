$proc = Start-Process -FilePath ".\bin\Debug\net8.0-windows\DigitalMosquito.exe" -PassThru
Start-Sleep -Seconds 3
if ($proc.HasExited) {
    Write-Host "Exited prematurely with code: $($proc.ExitCode)"
    exit 1
}
else {
    Write-Host "Application is running smoothly! PID: $($proc.Id)"
    Stop-Process -Id $proc.Id -Force
    exit 0
}
