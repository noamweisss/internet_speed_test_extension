# Publishing: what it takes to put the extension in the Store or in winget

Written for the owner. Plain words. The technical rules are in `docs/SECURITY.md`; the promises are in
`docs/SAFETY-CONTRACT.md`. Whether to publish at all is plan item 4.4 and stays the owner's decision.

## 1. Words

- **Compile**: turn the C# text into a program Windows can run.
- **Package**: wrap that program, its icon and its manifest into one file.
- **Build**: compile, then package. CI does both on every push.
- **MSIX**: the package file. Think of a box. Inside: the program and a manifest, the list of what the app asks
  Windows for (`internetClient`, `runFullTrust`). On the outside there is room for a seal.
- **Sign**: put the seal on the box. **Certificate**: what makes the seal. It has a public part anyone can check
  and a private key that must stay secret.
- Windows installs a sealed MSIX only when it trusts whoever issued the certificate. An unsealed MSIX it refuses,
  with one exception: Developer Mode lets a user register the unpacked folder for their own account.
- **Visual Studio**: an editor on top of the .NET SDK. The SDK compiles and packages. Visual Studio seals nothing
  on its own. Given a certificate, it does what one build flag does. GitHub's Windows machines run the same SDK.

## 2. What happens today

- CI (`.github/workflows/ci.yml`) runs `dotnet build` on a Windows runner with `AppxPackageSigningEnabled=false`.
  The result is an unsealed, self-contained MSIX: the .NET runtime is inside it, so the PC needs none.
- A release is a zip of that MSIX plus the install script.
- The owner runs `install/Install-SpeedTestExtension.ps1` in Developer Mode. It unpacks the MSIX into
  `%LOCALAPPDATA%\InternetSpeedTestExtension` and registers the folder for the current Windows user. No
  certificate, no administrator rights, no registry writes, removable by the same script (ADR-0008).
- This is fine for the owner and for people who read `docs/INSTALL.md`. It is not a product: a stranger would
  have to turn on Developer Mode and run a script.

## 3. Whose seal Windows trusts

| Seal | Who can get it | Cost | Trusted on a stranger's PC |
|------|----------------|------|----------------------------|
| A certificate from a certificate authority | Anyone who pays and proves identity | Hundreds of dollars a year | Yes |
| Microsoft's own, via the Microsoft Store | Anyone with a Partner Center account | Free for individuals since 2025-09-10 | Yes |
| Self-made | Anyone, one command | Free | No, only where someone installed it by hand |

## 4. Three roads, measured against the safety contract

| Road | What changes | What the contract says | Verdict |
|------|--------------|------------------------|---------|
| Buy a certificate, CI seals every build | The private key becomes a CI secret | "add this secret" is a red flag (`docs/SAFETY-CONTRACT.md` §2); a workflow that holds a secret and runs anything a pull request controls is P0 (`AGENTS.md`, Code Review Rules); ADR-0008 chose "no certificates" | Rejected |
| WinGet directly, following Microsoft's guide for Command Palette extensions | The MSIX goes away. An Inno Setup `.exe` installs into Program Files with administrator rights and writes the COM class into the registry | `install/` is "the script you run on your PC with your own rights" (§2); "needs admin" is a red flag; ADR-0008 chose per-user, no admin, no registry. The `.exe` still needs a trusted seal, or users see a SmartScreen warning | Rejected |
| Microsoft Store | The owner uploads the unsealed MSIX to Partner Center. Microsoft inspects and seals it. Per-user install stays. No secret in CI. `winget` installs Store apps too (`msstore` source), so winget comes for free | One change in the repository: the identity values from Partner Center go into `Package.appxmanifest`, a safety-sensitive file (§2, R13), with an ADR and a reviewed pull request | The only road that keeps the contract |

## 5. What a machine would and would not change

- **Visual Studio on any machine**: nothing. Same SDK, same unsealed box. Sealing needs a certificate, not a program.
- **A home server**: nothing for building or sealing. It could run the Windows test and the Windows App
  Certification Kit. On 2026-10-05 the owner decided the laptop's Hyper-V VM keeps that job (ADR-0019).
- **A self-hosted GitHub Actions runner**: never for this public repository. GitHub: self-hosted runners "should
  almost never be used for public repositories", because any user can open a pull request and compromise the
  environment.

## 6. If the owner says yes to the Store

For later. Each step names who does it.

1. Owner: register an individual Partner Center account. Free, no credit card, a government ID and a selfie.
2. Owner: reserve the app name in Partner Center.
3. Agent: put the identity values from Partner Center (`Identity Name`, `Publisher`, `PublisherDisplayName`) into
   `Package.appxmanifest` and the project file. ADR, pull request with a "Safety impact" section (R13).
4. CI: build the MSIX for x64, and for ARM64 if wanted; bundle them if more than one.
5. Optional, in the test VM: run the Windows App Certification Kit on the package. It predicts what the Store
   checks.
6. Owner: upload in Partner Center and fill the listing: description, screenshots, age rating, a privacy statement
   (the extension stores nothing and sends nothing but the test traffic, ADR-0007). The Store may ask why the
   package declares `runFullTrust`: it is a requirement of the Command Palette extension host.
7. Microsoft certifies, usually within days. After that, users install from the Store page or with `winget`.
8. Every new version is a new upload. Nothing in the code's rights changes because Microsoft sealed it.

## 7. Sources

- Microsoft Learn, "Publish Command Palette extensions" and its Store and WinGet pages, read 2026-09-28 (plan item
  4.4); confirmed 2026-10-05 against the `publish-extension` skill in the PowerToys repository, which describes
  the same two roads.
- Windows Developer Blog, "Free developer registration for individual developers on Microsoft Store",
  2025-09-10: no onboarding fee, no credit card, ID verification, nearly 200 markets.
- GitHub Docs, "Security hardening for GitHub Actions", section "Hardening for self-hosted runners", read 2026-10-05.
- ADR-0008 (install from an unsigned package in Developer Mode), ADR-0019 (the laptop VM stays the test surface),
  `docs/SAFETY-CONTRACT.md`.
