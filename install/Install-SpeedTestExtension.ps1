<#
.SYNOPSIS
    Installs, updates, or removes the Internet Speed Test extension for PowerToys Command Palette.

.DESCRIPTION
    Run it from the folder you downloaded from GitHub Actions (docs/INSTALL.md). It needs Windows Developer
    Mode, but no administrator rights, no certificate, and no Visual Studio (ADR-0008).

    What it changes on your PC, and nothing else:
    - the folder %LOCALAPPDATA%\InternetSpeedTestExtension (the extension's files), and
    - the app registration of the package "internet_speed_test_extension" for your Windows user.

.PARAMETER Uninstall
    Remove the extension and its folder instead of installing it.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Install-SpeedTestExtension.ps1
#>
[CmdletBinding()]
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'
$PackageName = 'internet_speed_test_extension'  # Identity Name in Package.appxmanifest
$InstallDir = Join-Path $env:LOCALAPPDATA 'InternetSpeedTestExtension'

if (-not $Uninstall) {
    $unlock = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue
    if ($unlock.AllowDevelopmentWithoutDevLicense -ne 1) {
        throw 'Developer Mode is off. Turn it on in Settings > System > For developers, then run this script again.'
    }
    $msix = @(Get-ChildItem -Path $PSScriptRoot -Filter '*.msix')
    if ($msix.Count -ne 1) {
        throw "Expected exactly one .msix file next to this script in $PSScriptRoot, found $($msix.Count)."
    }
}

# A registered folder cannot be replaced while registered, so an update is a removal followed by an install.
Get-AppxPackage -Name $PackageName | Remove-AppxPackage
if (Test-Path -LiteralPath $InstallDir) { Remove-Item -LiteralPath $InstallDir -Recurse -Force }
if ($Uninstall) {
    Write-Host 'Internet Speed Test extension removed.'
    return
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($msix[0].FullName, $InstallDir)
# Container metadata of the .msix file, not part of the app. Removing it leaves the same folder layout that
# Visual Studio registers when it deploys a build.
foreach ($name in 'AppxBlockMap.xml', 'AppxSignature.p7x', '[Content_Types].xml', 'AppxMetadata') {
    $path = Join-Path $InstallDir $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}
Add-AppxPackage -Register (Join-Path $InstallDir 'AppxManifest.xml')

Write-Host 'Installed. Open Command Palette (Win+Alt+Space), run "Reload", then search for "Internet Speed Test".'
