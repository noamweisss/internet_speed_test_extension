# ADR-0019: The laptop Hyper-V VM stays the test surface; no build, signing or runner machine

Status: accepted · Date: 2026-10-05

## Context
The owner has a second machine, a Dell OptiPlex 3020 from 2014 (Core i5-4590, 16 GB, Windows 10 Pro past end of
support, TPM 1.2, not eligible for Windows 11). They planned to set it up as a home server and, as its first job,
move the Hyper-V test VM `SpeedTest-Win11` there and run "the things needed to publish to winget". A planning
session on 2026-10-05 checked that plan against the facts:

- Building needs no machine. CI compiles and packages the MSIX with the .NET SDK on GitHub's Windows runners
  (`.github/workflows/ci.yml`). Visual Studio adds nothing: it is an editor over the same SDK.
- Signing needs a certificate, not a machine. The only certificate compatible with `docs/SAFETY-CONTRACT.md`
  and ADR-0008 is Microsoft's own, applied by the Store after a browser upload. The WinGet-direct road for
  Command Palette extensions (Inno Setup `.exe`, Program Files, administrator rights, registry) and a bought
  certificate held as a CI secret both break the contract. `docs/PUBLISHING.md` lays this out for the owner.
- A self-hosted GitHub Actions runner on this public repository is unsafe: GitHub's documentation says
  self-hosted runners "should almost never be used for public repositories".
- The server would therefore only replace the Hyper-V VM for manual test runs, at the cost of a Proxmox install
  on hardware Microsoft does not support for Windows 11 (a virtual TPM makes it work, but nothing is gained for
  publishing).

## Decision
1. The Hyper-V VM on the owner's laptop remains the proof surface (`docs/TESTING.md`, the capture harness from
   session 5). Nothing in the test workflow changes.
2. No self-hosted runner, now or later, while the repository is public.
3. No build or signing machine. Publishing, if the owner decides on it (plan item 4.4), goes through the Store:
   the owner uploads the CI-built MSIX, Microsoft signs it, `winget` finds it through the Store source.
4. The OptiPlex is set up, if at all, for the owner's other projects and documented in its own repository.

## Alternatives rejected
- Proxmox on the OptiPlex with a Windows 11 VM as the test surface. Works, gains nothing for publishing, costs
  an afternoon of physical access and a second thing to maintain. Open for later; a VM there would be a drop-in
  replacement for the Hyper-V VM.
- Windows 11 bare metal on the OptiPlex. Unsupported CPU and TPM 1.2; Microsoft removed the documented bypass in
  December 2024 and disclaims updates.
- Visual Studio on a VM "to build and sign". Visual Studio signs with a certificate the owner would still have
  to obtain; the certificate question is the whole question.

## Consequences
- `docs/PLAN.md` item 4.4 stays deferred and points at `docs/PUBLISHING.md`.
- If the owner later sets up the server, revisit point 1 of this ADR; points 2 and 3 stand regardless.
