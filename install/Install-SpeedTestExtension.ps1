<#
.SYNOPSIS
    Installs, updates, or removes the Internet Speed Test extension for PowerToys Command Palette.

.DESCRIPTION
    Run it from the folder you downloaded from GitHub Actions (docs/INSTALL.md). It needs Windows Developer
    Mode, but no administrator rights, no certificate, and no Visual Studio (ADR-0008).

    What it changes on your PC, and nothing else:
    - the folder %LOCALAPPDATA%\InternetSpeedTestExtension (the extension's files; an install unpacks into
      InternetSpeedTestExtension.new next to it first), and
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

    # Bound the unpack before writing anything: the declared sizes are what extraction writes.
    $zip = [System.IO.Compression.ZipFile]::OpenRead($msix[0].FullName)
    try {
        $unpackedBytes = ($zip.Entries | Measure-Object -Property Length -Sum).Sum
        if ($zip.Entries.Count -gt $MaxEntries -or $unpackedBytes -gt $MaxUnpackedBytes) {
            throw "The .msix is too large ($($zip.Entries.Count) files, $unpackedBytes bytes unpacked). Nothing was changed."
        }
    }
    finally { $zip.Dispose() }

    # Unpack and check the new package next to the installed one, so a broken or foreign .msix never costs the
    # working installation.
    $StagingDir = "$InstallDir.new"
    if (Test-Path -LiteralPath $StagingDir) { Remove-Item -LiteralPath $StagingDir -Recurse -Force }
    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($msix[0].FullName, $StagingDir)
        [xml]$manifest = Get-Content -LiteralPath (Join-Path $StagingDir 'AppxManifest.xml') -Raw
        if ($manifest.Package.Identity.Name -ne $PackageName) {
            throw "package name is '$($manifest.Package.Identity.Name)'"
        }
    }
    catch {
        if (Test-Path -LiteralPath $StagingDir) { Remove-Item -LiteralPath $StagingDir -Recurse -Force }
        throw "The .msix could not be unpacked or is not this extension ($($_.Exception.Message)). Nothing was changed."
    }
    # Container metadata of the .msix file, not part of the app. Removing it leaves the same folder layout that
    # Visual Studio registers when it deploys a build.
    foreach ($name in 'AppxBlockMap.xml', 'AppxSignature.p7x', '[Content_Types].xml', 'AppxMetadata') {
        $path = Join-Path $StagingDir $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Recurse -Force }
    }
}

# A registered folder cannot be replaced while registered, so an update is a removal followed by an install.
Get-AppxPackage -Name $PackageName | Remove-AppxPackage
if (Test-Path -LiteralPath $InstallDir) { Remove-Item -LiteralPath $InstallDir -Recurse -Force }
if ($Uninstall) {
    Write-Host 'Internet Speed Test extension removed.'
    return
}

Move-Item -LiteralPath $StagingDir -Destination $InstallDir
Add-AppxPackage -Register (Join-Path $InstallDir 'AppxManifest.xml')

Write-Host 'Installed. Open Command Palette (Win+Alt+Space), run "Reload", then search for "Internet Speed Test".'
