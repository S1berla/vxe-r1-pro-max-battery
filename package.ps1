param(
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$distRoot = Join-Path $root 'dist'
$releaseDir = Join-Path $distRoot ('v{0}\win-x64' -f $Version)
$zipPath = Join-Path $distRoot (
    'vxe-r1-pro-max-battery-v{0}-win-x64.zip' -f $Version
)

if (Test-Path -LiteralPath $releaseDir) {
    throw "Release directory already exists: $releaseDir"
}

& (Join-Path $root 'build.ps1')

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

$artifacts = @(
    'build\MouseBatteryTray.exe',
    'build\MouseBatteryTray.exe.config',
    'build\MouseBatteryConsole.exe',
    'README.md',
    'LICENSE'
)

foreach ($relativePath in $artifacts) {
    $source = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Packaging artifact not found: $source"
    }

    Copy-Item `
        -LiteralPath $source `
        -Destination (Join-Path $releaseDir (Split-Path $relativePath -Leaf)) `
        -Force
}

Copy-Item `
    -LiteralPath (Join-Path $root 'packaging\USAGE.txt') `
    -Destination (Join-Path $releaseDir 'USAGE.txt') `
    -Force
Copy-Item `
    -LiteralPath (Join-Path $root ('packaging\RELEASE-NOTES-v{0}.md' -f $Version)) `
    -Destination (Join-Path $releaseDir ('RELEASE-NOTES-v{0}.md' -f $Version)) `
    -Force

$hashFiles = @(
    'MouseBatteryTray.exe',
    'MouseBatteryTray.exe.config',
    'MouseBatteryConsole.exe',
    'USAGE.txt',
    'README.md',
    'LICENSE'
)

$hashLines = foreach ($fileName in $hashFiles) {
    $filePath = Join-Path $releaseDir $fileName
    $hash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
    '{0}  {1}' -f $hash, $fileName
}

$hashLines |
    Set-Content -LiteralPath (Join-Path $releaseDir 'SHA256SUMS.txt') `
        -Encoding utf8

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive `
    -Path (Join-Path $releaseDir '*') `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
'{0}  {1}' -f $zipHash, (Split-Path $zipPath -Leaf) |
    Set-Content -LiteralPath ($zipPath + '.sha256') -Encoding utf8

Write-Output ('RELEASE_DIR=' + $releaseDir)
Write-Output ('ZIP=' + $zipPath)
Write-Output ('ZIP_SHA256=' + $zipHash)
