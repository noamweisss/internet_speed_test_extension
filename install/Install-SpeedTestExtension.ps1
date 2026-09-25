<#
.SYNOPSIS
    Installs, updates, or removes the Internet Speed Test extension for PowerToys Command Palette.

.DESCRIPTION
    Run it from the folder you downloaded from GitHub Actions (docs/INSTALL.md). It needs Windows Developer
    Mode, but no administrator rights, no certificate, and no Visual Studio (ADR-0008).

    What it changes on your PC, and nothing else:
    - the folder %LOCALAPPDATA%\InternetSpeedTestExtension (the extension's files), and
    - the app registration of the package "InternetSpeedTestExtension" for your Windows user.

.PARAMETER Uninstall
    Remove the extension and its folder instead of installing it.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Install-SpeedTestExtension.ps1
#>
[CmdletBinding()]
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'
$PackageName = 'InternetSpeedTestExtension'  # Identity Name in Package.appxmanifest
$InstallDir = Join-Path $env:LOCALAPPDATA 'InternetSpeedTestExtension'
# Far above a real build (CI prints its file count and unpacked size); anything larger is not this extension.
$MaxEntries = 5000
$MaxUnpackedBytes = 500MB

Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not $Uninstall) {
    $unlock = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue
    if ($unlock.AllowDevelopmentWithoutDevLicense -ne 1) {
        throw 'Developer Mode is off. Turn it on in Settings > System > For developers, then run this script again.'
    }
    $msix = @(Get-ChildItem -Path $PSScriptRoot -Filter '*.msix')
    if ($msix.Count -ne 1) {
        throw "Expected exactly one .msix file next to this script in $PSScriptRoot, found $($msix.Count)."
    }

    # Check the new package before touching the installed one, so a corrupt, oversized, or foreign .msix leaves the
    # current installation as it is.
    $zip = [System.IO.Compression.ZipFile]::OpenRead($msix[0].FullName)
    try {
        $unpackedBytes = ($zip.Entries | Measure-Object -Property Length -Sum).Sum
        if ($zip.Entries.Count -gt $MaxEntries -or $unpackedBytes -gt $MaxUnpackedBytes) {
            throw "The .msix is too large ($($zip.Entries.Count) files, $unpackedBytes bytes unpacked). Nothing was changed."
        }
        $entry = $zip.GetEntry('AppxManifest.xml')
        if (-not $entry) { throw 'The .msix has no AppxManifest.xml. Nothing was changed.' }
        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($manifest.Package.Identity.Name -ne $PackageName) {
            throw "The .msix is not this extension (package name '$($manifest.Package.Identity.Name)'). Nothing was changed."
        }
    }
    finally { $zip.Dispose() }
}

# A registered folder cannot be replaced while registered, so an update is a removal followed by an install.
Get-AppxPackage -Name $PackageName | Remove-AppxPackage
if (Test-Path -LiteralPath $InstallDir) { Remove-Item -LiteralPath $InstallDir -Recurse -Force }
if ($Uninstall) {
    Write-Host 'Internet Speed Test extension removed.'
    return
}

[System.IO.Compression.ZipFile]::ExtractToDirectory($msix[0].FullName, $InstallDir)
# Container metadata of the .msix file, not part of the app. Removing it leaves the same folder layout that
# Visual Studio registers when it deploys a build.
foreach ($name in 'AppxBlockMap.xml', 'AppxSignature.p7x', '[Content_Types].xml', 'AppxMetadata') {
    $path = Join-Path $InstallDir $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}
Add-AppxPackage -Register (Join-Path $InstallDir 'AppxManifest.xml')

Write-Host 'Installed. Open Command Palette (Win+Alt+Space), run "Reload", then search for "Internet Speed Test".'
