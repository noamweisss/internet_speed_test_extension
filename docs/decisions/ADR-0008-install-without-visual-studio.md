# ADR-0008: Install from a CI-built, unsigned package in Developer Mode

Status: accepted · Date: 2026-09-24

## Context
The owner has VS Code, not Visual Studio, and must be able to install and test every build. The extension is
an MSIX-packaged COM server (the template's model; the unpackaged WinGet model is plan item 3.4). Windows installs an `.msix` file only if it is signed by a certificate
the PC trusts. The options were:

1. Sign with a self-made certificate and have the owner trust it. Rejected: trusting a certificate on the PC lets
   anything signed with it install silently, and the private key would have to live somewhere (a CI secret).
   `docs/SAFETY-CONTRACT.md` §5 promises "no certificates".
2. Mark the package as unsigned (a special publisher value) and install it with `Add-AppxPackage -AllowUnsigned`.
   Rejected for now: it changes the publisher identity in `Package.appxmanifest` (a safety-sensitive file) and
   gains nothing over option 3 for a single owner testing builds.
3. Register the unpacked package from a folder in Developer Mode, which is what Visual Studio does on
   Build > Deploy. The script needs no certificate and no administrator rights; Developer Mode is a Windows setting the
   owner switches (Windows may ask for administrator approval for that switch).

## Decision
Option 3. CI builds an unsigned, self-contained MSIX (the .NET runtime is inside it, so the PC needs none) and
uploads it with `install/Install-SpeedTestExtension.ps1` as an artifact. The script unpacks the MSIX into
`%LOCALAPPDATA%\InternetSpeedTestExtension` and registers it for the current user. The same script removes it
(`-Uninstall`). CI runs the script on a clean Windows runner on every push, so a broken install fails the build.

## Consequences
- Developer Mode must be on while installing. `docs/INSTALL.md` recommends Windows Sandbox first, and turning
  Developer Mode off again on a real PC after the test.
- The script runs on the owner's PC with their rights, so `install/` is a safety-sensitive path (R13) and is
  scanned for secrets (R4).
- Artifacts are only trustworthy from runs on `main` or on this repository's own branches; `docs/INSTALL.md`
  says which run to pick.
- Release builds (session 2, item 2.4) attach the same two files to a GitHub release. Store or WinGet
  distribution, which needs real signing, is a later decision (plan item 3.4).
