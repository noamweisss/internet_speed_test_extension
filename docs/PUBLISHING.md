# Publishing: what it takes to put the extension in the Store or in winget

Written for the owner. Plain words. The technical rules are in `docs/SECURITY.md`; the promises are in
`docs/SAFETY-CONTRACT.md`. Whether to publish at all is plan item 4.4 and stays the owner's decision.

Every statement about something outside this repository carries a confidence label and a source key: `Verified`
(read in the source named), `Inferred` (follows from a source, not stated there), `Unknown` (not found). The
sources are listed in section 7 as S1 to S12.

## 1. Words

- **Compile**: turn the C# text into a program Windows can run.
- **Package**: wrap that program, its icon and its manifest into one file.
- **Build**: compile, then package. CI does both on every push (Verified, S1).
- **MSIX**: the package file. Think of a box. Inside: the program and a manifest, the list of what the app asks
  Windows for (`internetClient`, `runFullTrust`). On the outside there is room for a seal.
- **Sign**: put the seal on the box. **Certificate**: what makes the seal. It has a public part anyone can check
  and a private key that must stay secret.
- Windows installs a sealed MSIX only when it trusts whoever issued the certificate (Verified, S2). An unsealed
  MSIX it refuses, with one exception: Developer Mode lets a user register the unpacked folder for their own
  account (Verified, S1: that is what the install script does).
- **Visual Studio**: an editor on top of the .NET SDK. The SDK compiles and packages. Visual Studio seals a
  package with a test certificate it generates, and that certificate must be installed by hand on every PC that
  should accept the package; Microsoft says not to distribute with it (Verified, S2). Given a real certificate,
  Visual Studio does what one build flag does. GitHub's Windows machines run the same SDK (Verified, S1).

## 2. What happens today

- CI (`.github/workflows/ci.yml`) runs `dotnet build` on a Windows runner with `AppxPackageSigningEnabled=false`.
  The result is an unsealed, self-contained MSIX: the .NET runtime is inside it, so the PC needs none (Verified, S1).
- A release is a zip of that MSIX plus the install script.
- The owner runs `install/Install-SpeedTestExtension.ps1` in Developer Mode. It unpacks the MSIX into
  `%LOCALAPPDATA%\InternetSpeedTestExtension` and registers the folder for the current Windows user. No
  certificate, no administrator rights, no registry writes, removable by the same script (ADR-0008).
- This is fine for the owner and for people who read `docs/INSTALL.md`. It is not a product: a stranger would
  have to turn on Developer Mode and run a script.

## 3. Whose seal Windows trusts

| Seal | Who can get it | Cost | Trusted on a stranger's PC | Confidence |
|------|----------------|------|----------------------------|------------|
| A certificate from a certificate authority | Anyone who pays and proves identity | A few hundred US dollars a year: reseller lists in 2026 start near 220 USD for Sectigo and 439 USD for DigiCert | Yes | Cost: Verified at resellers, prices vary (S4). Trust: Verified (S2) |
| Microsoft's own, via the Microsoft Store | Anyone with a Partner Center account | Free for individuals since 2025-09-10 | Yes | Verified (S3, S7) |
| Self-made | Anyone, one command | Free | No, only where someone installed it by hand | Verified (S2) |

## 4. Three roads, measured against the safety contract

| Road | What changes | What the contract says | Verdict |
|------|--------------|------------------------|---------|
| Buy a certificate, CI seals every build | The private key becomes a CI secret | "add this secret" is a red flag (`docs/SAFETY-CONTRACT.md` §2); a workflow that holds a secret and runs anything a pull request controls is P0 (`AGENTS.md`, Code Review Rules); ADR-0008 chose "no certificates" (S11) | Rejected |
| WinGet directly, following Microsoft's guide for Command Palette extensions | The project leaves MSIX (`WindowsPackageType` = `None`). An Inno Setup `.exe` installs per user, without asking for elevation, into the user's own Programs folder, and writes the extension's COM class under `HKEY_CURRENT_USER` (Verified, S5, S6) | No package manifest any more, so rule R12 and the "ask for `internetClient` + `runFullTrust`" row of §1 have nothing to check. The install script of ADR-0008 and `docs/INSTALL.md` give way to an installer. Registry writes are a "Yes" to question 2 of §3: allowed only with an ADR. The `.exe` carries no seal unless the owner buys a certificate (road 1); whether winget's silent install spares the user a SmartScreen warning is Unknown | Not chosen. It needs an ADR for the registry, drops the manifest the contract leans on, and ends at the same certificate question |
| Microsoft Store | The owner uploads the unsealed MSIX to Partner Center. Microsoft inspects and seals it (Verified, S7). Per-user install stays. No secret in CI. `winget` installs Store apps through its `msstore` source (Verified, S8), so winget comes for free | One change in the repository: the identity values from Partner Center go into `Package.appxmanifest` and the project file (Verified, S7), a safety-sensitive file (§2, R13), with an ADR and a reviewed pull request | The only road that keeps the contract |

Correction, 2026-10-07: the first version of this page, plan item 4.4 since 2026-09-28, and ADR-0019 said the
WinGet road installs into Program Files with administrator rights. That was wrong. The template's installer
sets `PrivilegesRequired=lowest` and `{autopf}`, which in Inno Setup means a per-user install with no elevation
(Verified, S5, S6).

## 5. What a machine would and would not change

- **Visual Studio on any machine**: nothing. Same SDK, same unsealed box (Verified, S1). Sealing needs a
  certificate, not a program (Verified, S2).
- **A home server**: nothing for building or sealing. It could run the Windows test and the Windows App
  Certification Kit. On 2026-10-05 the owner decided the laptop's Hyper-V VM keeps that job (ADR-0019).
- **A self-hosted GitHub Actions runner**: never for this public repository. GitHub: self-hosted runners "should
  almost never be used for public repositories", because any user can open a pull request and compromise the
  environment (Verified, S10).

## 6. If the owner says yes to the Store

For later. Each step names who does it. The steps follow the template's guide that ships in this repository (S7).

1. Owner: register an individual Partner Center account. Free, no credit card, a government ID and a selfie
   (Verified, S3).
2. Owner: reserve the app name in Partner Center and copy the three identity values from Product Identity
   (Verified, S7).
3. Agent: put those values (`Identity Name`, `Identity Publisher`, `PublisherDisplayName`) into
   `Package.appxmanifest` and the matching properties into the project file (Verified, S7). ADR, pull request
   with a "Safety impact" section (R13).
4. CI: build the MSIX for x64 and ARM64 and bundle them with `makeappx` from the Windows SDK (Verified, S7).
5. Optional, in the test VM: run the Windows App Certification Kit on the package. Microsoft's own pre-check
   before a Store submission; part of the Windows SDK; needs an administrator window and a signed-in session
   (Verified, S9).
6. Owner: upload the bundle in Partner Center and fill the listing: a description that names Command Palette,
   notes for certification that say PowerToys must be installed (Verified, S7), screenshots, age rating, and a
   privacy statement (the extension stores nothing and sends nothing but the test traffic, ADR-0007, S12).
   Whether the Store asks about `runFullTrust` is Unknown; the template requires it and Microsoft's own guide
   publishes such extensions to the Store, so an answer exists if asked (Inferred, S7).
7. Microsoft certifies, "typically 1–3 business days" (Verified, S7). After that, users install from the Store
   page or with `winget install <StoreId> -s msstore` (Verified, S8).
8. Every new version is a new submission with a higher package version (Verified, S7). Nothing in the code's
   rights changes because Microsoft sealed it.

## 7. Sources

- S1: `.github/workflows/ci.yml` and ADR-0008, this repository.
- S2: Microsoft Learn, "How to create a package signing certificate" and "Package a desktop or UWP app in
  Visual Studio": a package must be signed with a certificate the device trusts; Visual Studio's test
  certificate goes into the Trusted People store by hand and is not for distribution. Read 2026-10-07.
- S3: Windows Developer Blog, "Free developer registration for individual developers on Microsoft Store",
  2025-09-10: no onboarding fee, no credit card, ID verification, nearly 200 markets. Read 2026-10-05.
- S4: Reseller price comparisons for OV code-signing certificates, 2026 (sslinsights.com, "DigiCert vs Sectigo
  Code Signing"). Read 2026-10-07. Prices change; the order of magnitude is the point.
- S5: `.github/skills/publish-extension/references/winget-publishing.md`, the template's WinGet guide in this
  repository: `WindowsPackageType` `None`, `PrivilegesRequired=lowest`, `DefaultDirName={autopf}\...`, registry
  entries under `HKCU`.
- S6: Inno Setup help, "PrivilegesRequired" and "Constants" (jrsoftware.org/ishelp): `lowest` never requests
  elevation; `{autopf}` is the user's Program Files folder in non-administrative mode. Read 2026-10-07.
- S7: `.github/skills/publish-extension/references/store-publishing.md`, the template's Store guide in this
  repository: Partner Center identity values, x64 and ARM64 builds, `makeappx bundle`, notes for certification,
  "Certification typically takes 1–3 business days", one submission per version.
- S8: Microsoft Learn, "winget install command": the `--source` option and the example
  `winget install XP9KHM4BK9FZ7Q -s msstore`. Read 2026-10-07.
- S9: Microsoft Learn, "Windows App Certification Kit": validate locally before publication to the Store;
  included in the Windows SDK; run from an administrator command window inside an active user session. Read
  2026-10-07.
- S10: GitHub Docs, "Security hardening for GitHub Actions", section "Hardening for self-hosted runners". Read
  2026-10-05.
- S11: `docs/SAFETY-CONTRACT.md` §1 to §3 and `AGENTS.md`, "Code Review Rules", this repository.
- S12: ADR-0007, HTTPS transport metadata, this repository.
