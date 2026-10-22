$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "C# compiler not found: $compiler"
}

$outputDirectory = Join-Path $root 'build'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$consoleOutput = Join-Path $outputDirectory 'MouseBatteryConsole.exe'
$consoleSources = @(
    (Join-Path $root 'src\MouseBattery\HidNative.cs'),
    (Join-Path $root 'src\MouseBattery\VxeBatteryReader.cs'),
    (Join-Path $root 'src\MouseBattery.Console\Program.cs')
)

& $compiler `
    /nologo `
    /target:exe `
    /platform:x64 `
    /optimize+ `
    /out:$consoleOutput `
    $consoleSources

if ($LASTEXITCODE -ne 0) {
    throw "Console compilation failed with exit code $LASTEXITCODE"
}

$trayOutput = Join-Path $outputDirectory 'MouseBatteryTray.exe'
$trayManifest = Join-Path $root 'src\MouseBattery.Tray\app.manifest'
$traySources = @(
    (Join-Path $root 'src\MouseBattery\HidNative.cs'),
    (Join-Path $root 'src\MouseBattery\VxeBatteryReader.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\Program.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\AppLog.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\AutostartManager.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\BatteryIconRenderer.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\DetailsForm.cs'),
    (Join-Path $root 'src\MouseBattery.Tray\TrayApplicationContext.cs')
)

& $compiler `
    /nologo `
    /target:winexe `
    /platform:x64 `
    /optimize+ `
    /out:$trayOutput `
    /win32manifest:$trayManifest `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $traySources

if ($LASTEXITCODE -ne 0) {
    throw "Tray compilation failed with exit code $LASTEXITCODE"
}

Copy-Item `
    -LiteralPath (Join-Path $root 'src\MouseBattery.Tray\MouseBatteryTray.exe.config') `
    -Destination ($trayOutput + '.config') `
    -Force

Write-Output $consoleOutput
Write-Output $trayOutput

