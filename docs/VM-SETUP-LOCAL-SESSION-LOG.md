# Local session log: Hyper-V test VM

Temporary journal of the **local** Claude session on the owner's laptop, which set up the Hyper-V test VM for plan
item 2.2. It lives on branch `docs/local-vm-setup-log` and is kept apart from `docs/SESSION-LOG.md` on purpose, so
the two sessions never edit the same file. The cloud session may fold the relevant parts into `SESSION-LOG.md` /
`INSTALL.md` and then delete this file. Newest entry last.

Nothing else from the local session is committed. The VM scripts live only on the laptop, in
`C:\Users\Noam\SpeedTestVM\`; their full text is in the appendix so the cloud session can review or adopt them.

---

## Entry 1 — 2026-09-24 — why Windows Sandbox crashed

**Question:** Sandbox died twice with 0x80370106 ("virtual machine or container exited unexpectedly") while the
PowerToys installer hung. Was it memory?

**Answer: no, it was a kernel crash inside the Sandbox guest.**

- Hyper-V-Worker-Admin (needs an elevated reader) shows two Critical events 18590 at 22:38:26 and 22:41:35:
  the guest OS reported bugcheck **0x3B SYSTEM_SERVICE_EXCEPTION**, parameter 1 **0xC0000005** (access violation),
  faulting address **0xFFFFF8014E46800A** — the same address both times, so it is deterministic, not random.
- Hyper-V-CrashDump 40001 wrote guest dumps to `C:\ProgramData\Microsoft\Windows\Containers\Dumps\` (`c2764cd1-….dmp`,
  `7b0d509c-….dmp`). Not analysed (no debugger installed); the faulting driver is unknown.
- No memory pressure: Resource-Exhaustion-Detector only logged start/stop, no 2004 events. Laptop: 31.4 GB RAM,
  Intel Core Ultra 7 258V (8 cores), 254 GB free on C:.
- Each Sandbox start also logged Hyper-V-Worker 33101 "unsupported Virtual PCI protocol version 0x10006"; the VM
  kept running afterwards, so this is probably noise.
- Likely cause: Windows Sandbox runs the **host's own OS image**, and the laptop runs **Insider build 26300.9539
  (26H2)**. A kernel bug in that preview build, triggered during the PowerToys install, fits both identical crashes.
  A normal Hyper-V VM installs its own retail Windows, so it does not share that image.

## Entry 2 — 2026-09-24 — building the VM

- **ISO:** `Win11_25H2_English_x64_v2.iso` (7.89 GB) from microsoft.com/software-download/windows11 (owner approved
  the download). SHA-256 `768984706B909479417B2368438909440F2967FF05C6A9195ED2667254E465E3` matches the value
  Microsoft publishes on that page.
- **VM `SpeedTest-Win11`** (created by `New-SpeedTestVM.ps1`): Generation 2, 4 vCPU, dynamic memory 4 GB startup /
  2 GB min / 8 GB max, 80 GB dynamic VHDX, Secure Boot (MicrosoftWindows template) + vTPM (Windows 11 requires both),
  network on Hyper-V's **Default Switch**, Guest Service Interface enabled (for `Copy-VMFile`), checkpoint type
  Standard, automatic checkpoints off. Files in `C:\Users\Noam\SpeedTestVM\VM\`.
- The owner did Windows setup by hand (the local session does not create accounts):
  Windows 11 Pro, not activated, local administrator account `Noam` with a password, Developer Mode on,
  time zone Jerusalem (setup had defaulted to US Pacific; fixed before the final checkpoint).
- **Installed in the VM:** PowerToys **0.101.2652.0** (`PowerToysUserSetup-0.101.2652.0-x64.exe`, user install),
  Command Palette **0.12.12651.0**.
- **Guest build:** Windows 11 Pro 25H2 build **26200.8037** (read from the VM by the log script).
- **Checkpoint:** `clean-powertoys-devmode`, Standard, taken with the VM running at an idle desktop after
  PowerToys, Developer Mode and the time zone were set.
- The laptop itself has the owner's own PowerToys 0.101.2362.0 (installed 2026-09-02, daily use) and some Store
  Command Palette extensions. They were not touched. The speed-test extension is **not** installed on the laptop and
  must only ever be installed in the VM.
- Bug found and fixed while setting up: `Send-ToSpeedTestVM.ps1` first declared `$Path` with
  `ValueFromRemainingArguments`, which gives it no position, so the file path bound to `-VMName` and PowerShell
  prompted for `Path`. Now `Position = 0`.
- Registry `ProductName` says "Windows 10" on Windows 11; the log script corrects it for builds ≥ 22000.

## Entry 3 — 2026-09-25 — VM had no internet (fixed)

**Symptom:** after restoring the checkpoint the next morning, the VM had no internet.

**Cause:** the restored VM still held the DHCP lease from when the checkpoint was taken (172.20.x.x/20, gateway
172.20.0.1). Overnight the laptop re-created the Default Switch (Host-Network-Service events at 04:25–04:27) on a new
range, 172.18.48.0/20: the laptop had moved to a phone hotspot that uses 172.20.10.x, which clashes with the old range.
`ipconfig /renew` alone timed out because it asks the DHCP server to keep the old address.
`ipconfig /release` followed by `ipconfig /renew` got a 172.18.x.x address and the browser in the VM worked.

**Fix:** `Reset-SpeedTestVM.ps1` now, after every restore, runs `ipconfig /release` and `ipconfig /renew` inside the VM
over PowerShell Direct and prints the new address. The owner ran it: `VM network address: 172.18.57.7`. The laptop's
network configuration was not changed; the Default Switch keeps NAT-ing through whatever network the laptop uses
(home Wi-Fi, hotspot, work). If the laptop changes networks while the VM is running, run the same two commands in
the VM.

Not changed (fallback if the Default Switch ever stops handing out addresses): a dedicated internal switch with
`New-NetNat` and a static IP in the VM. That would be a permanent host network change, so it needs the owner's approval.

## Entry 4 — 2026-09-25 — VM power

- `Set-VM -AutomaticStartAction Nothing`: the VM no longer starts by itself when the laptop boots.
- Shut down with `Stop-VM -Name SpeedTest-Win11`.
- Start a test session with `.\Reset-SpeedTestVM.ps1 -SkipLogs`: restores the clean checkpoint (a saved running
  state, so the desktop is back in seconds), starts it, renews the address.

---

## How the owner drives the VM

Everything runs **on the laptop**, in an **administrator PowerShell**, in `C:\Users\Noam\SpeedTestVM`:

| Task | Command |
|------|---------|
| Start a clean VM for testing | `.\Reset-SpeedTestVM.ps1 -SkipLogs` |
| Open the VM window | `vmconnect.exe localhost SpeedTest-Win11` |
| Open it with display settings first | `vmconnect.exe localhost SpeedTest-Win11 /edit` |
| Copy a file onto the VM desktop | `.\Send-ToSpeedTestVM.ps1 "<path on laptop>"` (lands in `C:\Users\Public\Desktop\`) |
| Save logs from the VM to the laptop | `.\Get-SpeedTestVMLogs.ps1` |
| Save logs, revert to clean, renew network | `.\Reset-SpeedTestVM.ps1` |
| Continue a half-done session (no revert) | `Start-VM -Name SpeedTest-Win11` |
| Shut the VM down | `Stop-VM -Name SpeedTest-Win11` |

The scripts that talk to the guest use PowerShell Direct (VMBus, no network) with the VM account, prompted once and
saved in `vm-credential.xml`, encrypted with Windows DPAPI for the owner's laptop account. They need the VM running.

### One test cycle (plan item 2.2)

1. Laptop: download the CI artifact zip (`docs/INSTALL.md` §1), then
   `.\Send-ToSpeedTestVM.ps1 "$env:USERPROFILE\Downloads\<artifact>.zip"`.
2. VM: right-click the zip on the desktop → Extract All → open the folder → right-click an empty area →
   Open in Terminal → `powershell -ExecutionPolicy Bypass -File .\Install-SpeedTestExtension.ps1`
   (`docs/INSTALL.md` 2A steps 5–7; PowerToys and Developer Mode are already in the checkpoint).
3. VM: Win+Alt+Space → Reload → search "Internet Speed Test" → `docs/TESTING.md` checklist.
4. Laptop: `.\Reset-SpeedTestVM.ps1` — saves the logs, reverts, renews the network.

### What the logs contain

Saved on the laptop in `C:\Users\Noam\SpeedTestVM\Logs\<yyyy-MM-dd_HH-mm-ss>\`, so they survive a revert:

- `summary.txt` — Windows build, PowerToys version, Developer Mode, `Get-AppxPackage` for PowerToys / Command
  Palette / InternetSpeedTest (version, status, install location).
- `EventLogs\errors-and-warnings.txt` — errors and warnings of the last 24 h (`-Hours N` to change) from
  AppXDeploymentServer, AppXDeployment, AppxPackaging, AppModel-Runtime, TWinUI, Application, System; the same logs
  as `.evtx`.
- `PowerToys\` — `%LOCALAPPDATA%\Microsoft\PowerToys` (`*.log`, `*.txt`, `*.json`).
- `Packages\` — the same file types from the Command Palette, PowerToys and extension package folders.
- `Installer\` — PowerToys installer logs from `%TEMP%`.
- `WER\`, `CrashDumps\` — crash reports / dumps whose names match the extension, PowerToys or Command Palette.

First real run (no extension installed yet): 267 warning/error lines, 73 PowerToys files, ~1 MB. The owner pastes
`summary.txt` and `errors-and-warnings.txt` back to the cloud session.

### Why the scripts are not in the repo

AGENTS.md §3: no tooling that only works on one OS unless CI covers it. These are Hyper-V-only and cannot run in CI.
The cloud session decides whether to adopt them (e.g. under `install/vm/` with an ADR) or only document the VM path
in `INSTALL.md`.

## Appendix: script sources (as on the laptop, 2026-09-25)

### `New-SpeedTestVM.ps1`

```powershell
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Creates the Hyper-V test VM for the Command Palette speed-test extension and boots it from the Windows 11 ISO.
.NOTES
  Run once. After Windows setup, PowerToys and Developer Mode, run:
    Checkpoint-VM -Name SpeedTest-Win11 -SnapshotName clean-powertoys-devmode
#>
param(
    [string]$VMName = 'SpeedTest-Win11',
    [string]$IsoPath = "$env:USERPROFILE\Downloads\Win11_25H2_English_x64_v2.iso",
    [string]$VMRoot = "$PSScriptRoot\VM"
)
$ErrorActionPreference = 'Stop'

if (Get-VM -Name $VMName -ErrorAction SilentlyContinue) { throw "VM '$VMName' already exists." }
if (-not (Test-Path $IsoPath)) { throw "ISO not found: $IsoPath" }

New-Item -ItemType Directory -Force $VMRoot | Out-Null
$vhd = Join-Path $VMRoot "$VMName.vhdx"

# Gen 2 + vTPM + Secure Boot: Windows 11 setup refuses to install without them.
New-VM -Name $VMName -Generation 2 -Path $VMRoot -NewVHDPath $vhd -NewVHDSizeBytes 80GB `
       -MemoryStartupBytes 4GB -SwitchName 'Default Switch' | Out-Null
Set-VM -Name $VMName -ProcessorCount 4 -DynamicMemory -MemoryMinimumBytes 2GB -MemoryMaximumBytes 8GB `
       -CheckpointType Standard -AutomaticCheckpointsEnabled $false -AutomaticStopAction ShutDown
Set-VMFirmware -VMName $VMName -EnableSecureBoot On -SecureBootTemplate MicrosoftWindows
Set-VMKeyProtector -VMName $VMName -NewLocalKeyProtector
Enable-VMTPM -VMName $VMName
# Lets Copy-VMFile push installers and builds into the VM without guest credentials.
Enable-VMIntegrationService -VMName $VMName -Name 'Guest Service Interface'

$dvd = Add-VMDvdDrive -VMName $VMName -Path $IsoPath -Passthru
Set-VMFirmware -VMName $VMName -FirstBootDevice $dvd

Start-VM -Name $VMName
Start-Process vmconnect.exe -ArgumentList 'localhost', $VMName
Write-Host "VM '$VMName' started. In the VM window, press any key when it says 'Press any key to boot from CD or DVD'."
```

### `Send-ToSpeedTestVM.ps1`

```powershell
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Copies files from this laptop onto the Public Desktop of the test VM (visible to every user in the VM).
.EXAMPLE
  .\Send-ToSpeedTestVM.ps1 "$env:USERPROFILE\Downloads\internet-speed-test-extension-x64.zip"
.EXAMPLE
  .\Send-ToSpeedTestVM.ps1 first.zip, second.exe
#>
param(
    [Parameter(Mandatory, Position = 0)][string[]]$Path,
    [string]$VMName = 'SpeedTest-Win11'
)
$ErrorActionPreference = 'Stop'

foreach ($file in $Path) {
    $item = Get-Item $file
    Copy-VMFile -Name $VMName -SourcePath $item.FullName -FileSource Host -CreateFullPath -Force `
                -DestinationPath "C:\Users\Public\Desktop\$($item.Name)"
    Write-Host "Sent $($item.Name)"
}
```

### `Get-SpeedTestVMLogs.ps1`

```powershell
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Collects PowerToys / Command Palette logs, AppX install errors and crash reports from the test VM
  and saves them on this laptop under .\Logs\<timestamp>, so they survive a checkpoint revert.
.NOTES
  Uses PowerShell Direct (no network needed). First run asks for the VM user's name and password and saves
  them to vm-credential.xml, encrypted so only your Windows account on this laptop can read them.
  Delete that file if the VM password changes.
#>
param(
    [string]$VMName = 'SpeedTest-Win11',
    [string]$LogRoot = "$PSScriptRoot\Logs",
    [int]$Hours = 24
)
$ErrorActionPreference = 'Stop'

$credFile = "$PSScriptRoot\vm-credential.xml"
if (Test-Path $credFile) { $cred = Import-Clixml $credFile }
else {
    $cred = Get-Credential -Message "User name and password inside the VM (for a local account: .\name)"
    $cred | Export-Clixml $credFile
}

if ((Get-VM -Name $VMName).State -ne 'Running') { throw "VM '$VMName' is not running. Start it first." }
try { $session = New-PSSession -VMName $VMName -Credential $cred }
catch { Remove-Item $credFile -ErrorAction SilentlyContinue; throw "Could not log in to the VM (saved credential removed, run again): $_" }

try {
    $zipInVm = Invoke-Command -Session $session -ArgumentList $Hours -ScriptBlock {
        param($Hours)
        $ErrorActionPreference = 'Continue'
        $stage = Join-Path $env:TEMP 'speedtest-logs'
        Remove-Item $stage, "$stage.zip" -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory $stage | Out-Null
        $since = (Get-Date).AddHours(-$Hours)

        function Copy-Tree($source, $target, [string[]]$include) {
            if (-not (Test-Path $source)) { return }
            Get-ChildItem $source -Recurse -File -Include $include -ErrorAction SilentlyContinue | ForEach-Object {
                $dest = Join-Path $target $_.FullName.Substring($source.Length).TrimStart('\')
                New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
                Copy-Item $_.FullName $dest -ErrorAction SilentlyContinue
            }
        }

        # Summary: build, PowerToys version, Developer Mode, installed packages.
        $cv = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
        # The registry still says "Windows 10" on Windows 11; build 22000+ is Windows 11.
        if ([int]$cv.CurrentBuild -ge 22000) { $cv.ProductName = $cv.ProductName -replace 'Windows 10', 'Windows 11' }
        $devMode = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
        $pt = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*', 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue |
              Where-Object DisplayName -like 'PowerToys*' | Select-Object -First 1
        @(
            "Collected:      $(Get-Date -Format s)"
            "Windows:        $($cv.ProductName) $($cv.DisplayVersion) build $($cv.CurrentBuild).$($cv.UBR)"
            "PowerToys:      $($pt.DisplayVersion)"
            "Developer Mode: $(if ($devMode -eq 1) { 'on' } else { 'off' })"
            ''
            Get-AppxPackage | Where-Object Name -match 'PowerToys|CommandPalette|InternetSpeedTest' |
                Format-List Name, Version, Status, IsDevelopmentMode, InstallLocation | Out-String
        ) | Set-Content "$stage\summary.txt"

        # PowerToys and Command Palette logs, including the installer's logs in %TEMP%.
        Copy-Tree "$env:LOCALAPPDATA\Microsoft\PowerToys" "$stage\PowerToys" '*.log', '*.txt', '*.json'
        Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -ErrorAction SilentlyContinue |
            Where-Object Name -match 'CommandPalette|PowerToys|InternetSpeedTest' |
            ForEach-Object { Copy-Tree $_.FullName "$stage\Packages\$($_.Name)" '*.log', '*.txt', '*.json' }
        New-Item -ItemType Directory "$stage\Installer" | Out-Null
        Get-ChildItem $env:TEMP -File -Filter '*PowerToys*' -ErrorAction SilentlyContinue | Copy-Item -Destination "$stage\Installer"

        # Event logs: full .evtx for Event Viewer, plus a readable text file of errors and warnings.
        New-Item -ItemType Directory "$stage\EventLogs" | Out-Null
        $logs = 'Microsoft-Windows-AppXDeploymentServer/Operational', 'Microsoft-Windows-AppXDeployment/Operational',
                'Microsoft-Windows-AppxPackaging/Operational', 'Microsoft-Windows-AppModel-Runtime/Admin',
                'Microsoft-Windows-TWinUI/Operational', 'Application', 'System'
        $ms = [int]((Get-Date) - $since).TotalMilliseconds
        foreach ($log in $logs) {
            $file = "$stage\EventLogs\$($log -replace '[/ ]', '_').evtx"
            wevtutil epl $log $file "/q:*[System[TimeCreated[timediff(@SystemTime) <= $ms]]]" 2>$null
        }
        # One query per log: a single missing log would otherwise fail the whole query.
        $logs | ForEach-Object { Get-WinEvent -FilterHashtable @{ LogName = $_; Level = 1, 2, 3; StartTime = $since } -ErrorAction SilentlyContinue } |
            Sort-Object TimeCreated |
            ForEach-Object { '{0:s} [{1}] {2} {3} ({4}): {5}' -f $_.TimeCreated, $_.LevelDisplayName, $_.LogName, $_.Id, $_.ProviderName, ($_.Message -replace '\s+', ' ') } |
            Set-Content "$stage\EventLogs\errors-and-warnings.txt"

        # Crash reports for the extension, PowerToys and Command Palette.
        $crashPattern = 'internet_speed_test|InternetSpeedTest|PowerToys|CmdPal|CommandPalette'
        New-Item -ItemType Directory "$stage\WER" | Out-Null
        foreach ($wer in "$env:ProgramData\Microsoft\Windows\WER", "$env:LOCALAPPDATA\Microsoft\Windows\WER") {
            Get-ChildItem "$wer\ReportArchive", "$wer\ReportQueue" -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match $crashPattern } |
                ForEach-Object { Copy-Item $_.FullName "$stage\WER\$($_.Name)" -Recurse -Force -ErrorAction SilentlyContinue }
        }
        Get-ChildItem "$env:LOCALAPPDATA\CrashDumps" -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match $crashPattern } |
            ForEach-Object { New-Item -ItemType Directory -Force "$stage\CrashDumps" | Out-Null; Copy-Item $_.FullName "$stage\CrashDumps" }

        Compress-Archive -Path "$stage\*" -DestinationPath "$stage.zip" -Force
        "$stage.zip"
    }

    $target = Join-Path $LogRoot (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item -FromSession $session -Path $zipInVm -Destination "$target.zip"
    Expand-Archive "$target.zip" -DestinationPath $target
    Remove-Item "$target.zip"
    Write-Host "Logs saved to $target"
    Get-Content "$target\summary.txt" | Select-Object -First 4 | Write-Host
}
finally { Remove-PSSession $session }
```

### `Reset-SpeedTestVM.ps1`

```powershell
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Saves the VM's logs to this laptop, reverts the VM to the clean checkpoint, and gives it a fresh network address.
.PARAMETER SkipLogs
  Revert without collecting logs first.
#>
param(
    [string]$VMName = 'SpeedTest-Win11',
    [string]$Checkpoint = 'clean-powertoys-devmode',
    [switch]$SkipLogs
)
$ErrorActionPreference = 'Stop'

if (-not $SkipLogs -and (Get-VM -Name $VMName).State -eq 'Running') {
    & "$PSScriptRoot\Get-SpeedTestVMLogs.ps1" -VMName $VMName
}
Restore-VMCheckpoint -VMName $VMName -Name $Checkpoint -Confirm:$false
# A checkpoint taken while running restores to a saved state; starting resumes the desktop.
if ((Get-VM -Name $VMName).State -ne 'Running') { Start-VM -Name $VMName }
Write-Host "VM '$VMName' is back at '$Checkpoint'."

# The restored VM still holds the network address it had when the checkpoint was taken. The Default Switch picks
# a new address range after a laptop restart or a network change, so the old address leads nowhere. Release it
# and ask for a fresh one; renew alone would keep asking for the old address and time out.
$credFile = "$PSScriptRoot\vm-credential.xml"
if (-not (Test-Path $credFile)) {
    Write-Warning "No saved VM login (run Get-SpeedTestVMLogs.ps1 once). In the VM run: ipconfig /release; ipconfig /renew"
    return
}
$job = Invoke-Command -VMName $VMName -Credential (Import-Clixml $credFile) -AsJob -ScriptBlock {
    ipconfig /release | Out-Null
    ipconfig /renew | Out-Null
    (Get-NetIPConfiguration | Where-Object IPv4DefaultGateway | Select-Object -First 1).IPv4Address.IPAddress
}
if (Wait-Job $job -Timeout 90) {
    $address = Receive-Job $job -ErrorAction SilentlyContinue
    if ($address) { Write-Host "VM network address: $address" }
    else { Write-Warning "VM got no network address. In the VM run: ipconfig /release; ipconfig /renew" }
}
else { Write-Warning "VM did not answer within 90 s. In the VM run: ipconfig /release; ipconfig /renew" }
Remove-Job $job -Force
```
