param(
    [switch]$Watch,
    [switch]$Json,
    [int]$Interval = 60
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$executable = Join-Path $root 'build\MouseBatteryConsole.exe'

if (-not (Test-Path -LiteralPath $executable)) {
    throw "Build the console reader first: powershell -ExecutionPolicy Bypass -File .\build.ps1"
}

$arguments = @()
if ($Watch) {
    $arguments += '--watch'
    $arguments += '--interval'
    $arguments += $Interval
}
if ($Json) {
    $arguments += '--json'
}

& $executable @arguments
exit $LASTEXITCODE
