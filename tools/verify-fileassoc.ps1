# ============================================================================
# Tittle -- read-only Markdown association verification (current user)
# ============================================================================
# Checks the HKCU association written by install-fileassoc.ps1. This script never
# creates, deletes, or changes registry keys, so it is safe to run in CI or after
# choosing another application in Explorer.
#
#   .\tools\verify-fileassoc.ps1
#   .\tools\verify-fileassoc.ps1 -ExePath 'C:\Apps\Tittle\Tittle.exe'
# ============================================================================

param(
    [string[]]$Extensions = @('.md', '.markdown'),
    [string]$ExePath = ''
)

$ErrorActionPreference = 'Stop'
$progId = 'Tittle.Markdown'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$exe = if ($ExePath) { [System.IO.Path]::GetFullPath($ExePath) } else {
    Join-Path $root 'dist\Tittle.exe'
}
$expectedCommand = '"{0}" "%1"' -f $exe
$failures = [System.Collections.Generic.List[string]]::new()

function Read-RegistryDefault([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-ItemProperty -LiteralPath $Path -Name '(Default)' -ErrorAction SilentlyContinue).'(Default)'
}

function Require-Equal([string]$Label, [object]$Actual, [object]$Expected) {
    if ($Actual -ne $Expected) {
        $failures.Add("${Label}: expected '$Expected', got '$Actual'")
    } else {
        Write-Host "  [OK] $Label" -ForegroundColor Green
    }
}

Write-Host ''
Write-Host '  Verify Markdown -> Tittle association (read-only)' -ForegroundColor Cyan
Write-Host "  exe: $exe" -ForegroundColor DarkGray

Require-Equal 'ProgId display name' (Read-RegistryDefault "HKCU:\SOFTWARE\Classes\$progId") 'Markdown Document'
Require-Equal 'ProgId open command' (Read-RegistryDefault "HKCU:\SOFTWARE\Classes\$progId\shell\open\command") $expectedCommand

foreach ($ext in $Extensions) {
    Require-Equal "$ext default ProgId" (Read-RegistryDefault "HKCU:\SOFTWARE\Classes\$ext") $progId

    $userChoicePath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\$ext\UserChoice"
    $chosen = (Get-ItemProperty -LiteralPath $userChoicePath -Name 'ProgId' -ErrorAction SilentlyContinue).ProgId
    if ($chosen) {
        Require-Equal "$ext UserChoice ProgId" $chosen $progId
    }
}

$appKey = 'HKCU:\SOFTWARE\Classes\Applications\Tittle.exe'
Require-Equal 'Open-with friendly name' (Get-ItemProperty -LiteralPath $appKey -Name FriendlyAppName -ErrorAction SilentlyContinue).FriendlyAppName 'Tittle'
Require-Equal 'Open-with command' (Read-RegistryDefault "$appKey\shell\open\command") $expectedCommand
foreach ($ext in $Extensions) {
    $supported = Get-ItemProperty -LiteralPath "$appKey\SupportedTypes" -Name $ext -ErrorAction SilentlyContinue
    if ($null -eq $supported) {
        $failures.Add("Open-with SupportedTypes is missing '$ext'")
    } else {
        Write-Host "  [OK] Open-with supports $ext" -ForegroundColor Green
    }
}

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host '  Association verification failed:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  [FAIL] $_" -ForegroundColor Red }
    exit 1
}

Write-Host '  Association verification passed. No registry values were changed.' -ForegroundColor Green
Write-Host ''
