$ErrorActionPreference = 'Stop'

$processes = @(Get-Process -Name 'MouseBatteryTray' -ErrorAction SilentlyContinue)
Write-Output ('PROCESS_COUNT={0}' -f $processes.Count)
$processes |
    Select-Object Id, StartTime, Path |
    Format-Table -AutoSize |
    Out-String -Width 220 |
    Write-Output

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$settingsKey = 'HKCU:\Software\VxeMouseBatteryTray'
$runValue = $null
$configured = $null

if (Test-Path -LiteralPath $runKey) {
    $runValue = (Get-ItemProperty `
        -LiteralPath $runKey `
        -Name 'VxeMouseBatteryTray' `
        -ErrorAction SilentlyContinue).VxeMouseBatteryTray
}

if (Test-Path -LiteralPath $settingsKey) {
    $configured = (Get-ItemProperty `
        -LiteralPath $settingsKey `
        -ErrorAction SilentlyContinue).AutostartConfigured
}

Write-Output ('AUTOSTART_COMMAND={0}' -f $runValue)
Write-Output ('AUTOSTART_CONFIGURED={0}' -f $configured)

$logPath = Join-Path $env:LOCALAPPDATA 'VxeMouseBatteryTray\app.log'
Write-Output ('LOG_PATH={0}' -f $logPath)

if (Test-Path -LiteralPath $logPath) {
    Get-Content -LiteralPath $logPath -Tail 20 |
        Out-String -Width 240 |
        Write-Output
}
else {
    Write-Output 'LOG_NOT_FOUND'
}
