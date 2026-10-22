param(
    [switch]$Test
)

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$executable = Join-Path $root 'build\MouseBatteryTray.exe'

if (-not (Test-Path -LiteralPath $executable)) {
    throw 'Build the tray program first with build.ps1.'
}

if ($Test) {
    & $executable --test
    exit $LASTEXITCODE
}

Start-Process -FilePath $executable
