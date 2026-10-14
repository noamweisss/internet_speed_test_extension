# Local development

How to build the extension on your own Windows PC instead of waiting for CI, and how to test the result safely.
CI stays the reference build (`.github/workflows/ci.yml`); this page runs the same commands by hand.

Written for the owner's laptop (Windows 11, x64, VS Code, .NET SDK) on 2026-10-14; every number below was
measured there.

## 1. What you need

| Tool | Why | Install |
|------|-----|---------|
| .NET SDK 10 | Compiles the code and runs the tests | `winget install Microsoft.DotNet.SDK.10` |
| Windows SDK 10.0.26100 | The extension talks to Windows through WinRT; the build reads Windows' API descriptions from this SDK. Without it the build stops with `Could not find the Windows SDK in the registry` | See below |
| VS Code (optional) | Editing. Not needed to build | already installed |

Full Visual Studio (Community) is **not** needed. Two ways to get the Windows SDK:

- **Visual Studio Build Tools with only the SDK component** (what the owner's laptop uses). Command-line build kit,
  no editor. Downloads over HTTPS only.

  ```powershell
  winget install Microsoft.VisualStudio.2022.BuildTools --override "--quiet --wait --norestart --add Microsoft.VisualStudio.Component.Windows11SDK.26100"
  ```

- **The standalone Windows SDK installer**: `winget install Microsoft.WindowsSDK.10.0.26100`. Smaller, but its
  installer downloads its parts over plain `http://`. On a network that blocks plain HTTP (the owner's does: even
  `http://example.com` answers 403), it fails with exit code 15605 and `HTTP Return Code: 403` in
  `%TEMP%\windowssdk\*.log`. Use Build Tools there.

Both need administrator approval once (a UAC prompt). Neither installs a service or opens a port.

After installing, open a new terminal so `PATH` is fresh.

## 2. Build

Run everything from the repository folder.

**Core tests** (fast, any OS):

```powershell
dotnet test tests/SpeedTest.Core.Tests
```

**Extension and MSIX package** (the CI command plus one property, Windows only):

```powershell
dotnet build internet_speed_test_extension/internet_speed_test_extension.csproj -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:SelfContained=true -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -p:AppxBundle=Never -p:AppxPackageDir=AppPackages/ -p:DefaultLanguage=en-US
```

The one difference from CI is `-p:DefaultLanguage=en-US`. The manifest asks the build to pick the package
language (`x-generate`), and the build takes it from the PC's language settings (inferred from the error). On
the owner's laptop that is `en-IL`, which MSIX rejects: `error APPX1685: Default language 'en-IL' is not valid`. CI runs in `en-US`, so it
never sees this.

The package lands in
`internet_speed_test_extension\AppPackages\internet_speed_test_extension_<version>_x64_Test\internet_speed_test_extension_<version>_x64.msix`
(ignored by git). It is unsigned, like the CI build. To install it with the normal script, put the `.msix` and
`install/Install-SpeedTestExtension.ps1` in one folder; that folder is the same as a CI artifact zip.

## 3. Disk and CPU

Measured on the owner's laptop (Intel Core Ultra 7 258V, 8 cores):

| Item | Size or time |
|------|--------------|
| Build Tools with the Windows SDK component | 1.9 GB (Windows SDK 1.7 GB, Build Tools and its installer 0.2 GB) |
| NuGet packages after the first restore (`%USERPROFILE%\.nuget\packages`) | 1.7 GB, shared by every .NET project on the PC |
| `bin` and `obj` after one Release build | 0.15 GB |
| The `.msix` | 14 MB |
| First extension build | 26 seconds with the NuGet packages already downloaded; on a fresh PC add the 1.7 GB download |
| Next builds | 4 seconds when nothing changed; Core tests (154) in 11 seconds |

The build uses all cores for a short time; the fan may spin up. MSBuild helper processes stay idle for a few minutes afterwards, then exit.

## 4. Test the build in Command Palette

Do not install a local build on the PC you use every day: a broken build can stop Command Palette from loading
extensions (ADR-0019 keeps a VM as the test surface). Use one of these:

- **Windows Sandbox**: a disposable Windows inside your PC. Close it and everything inside is deleted. Follow
  `docs/INSTALL.md` §2A with your local folder instead of the downloaded zip. Sandbox runs the same Windows build
  as your PC; on a Windows Insider build it may crash (`docs/INSTALL.md` explains).
- **The Hyper-V VM** (`docs/INSTALL.md` §2C): keeps its state and has a released Windows. The owner's VM is rebuilt
  from the recipe outside this repository when needed.

Then run the checklist in `docs/TESTING.md`.

## 5. Safety notes

- **Outbound, not inbound.** Building connects out to `api.nuget.org` over HTTPS to download packages, and the
  installers connect out to Microsoft. Outbound means your PC starts the connection, like a browser. Nothing
  here needs another computer to connect in to yours.
- **Firewall prompts.** If Windows Defender Firewall asks "Allow access?" for `dotnet`, `MSBuild` or anything
  else during a build, click **Cancel**. Building and testing never need an inbound rule. Never open a port for
  this project.
- **Check that nothing listens.** After a build, this lists every program waiting for incoming connections:

  ```powershell
  Get-NetTCPConnection -State Listen | Select-Object LocalAddress, LocalPort, OwningProcess, @{n='Process'; e={(Get-Process -Id $_.OwningProcess).ProcessName}} | Sort-Object LocalPort
  ```

  Run it once before you install anything, keep the output, and compare after. A `dotnet` or `MSBuild` line that
  was not there before is wrong: close the terminal and tell an agent. (MSBuild keeps helper processes for a
  few minutes after a build; they talk through named pipes, not network ports, so they do not appear here.)
- **Only well-known installers.** Everything on this page comes from Microsoft through `winget`, which checks
  each installer's SHA-256 before running it.
