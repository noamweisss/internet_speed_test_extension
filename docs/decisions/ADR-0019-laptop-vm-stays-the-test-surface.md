# ADR-0019: The laptop Hyper-V VM stays the test surface; no build, signing or runner machine

Status: accepted · Date: 2026-10-05 · Corrected 2026-10-07 (the WinGet road installs per user, not with
administrator rights)

Statements about systems outside this repository carry a confidence label (`Verified`, `Inferred`, `Unknown`)
and name their source. `docs/PUBLISHING.md` §7 holds the full source list; its keys (S1 to S12) are reused here.

## Context
The owner has a second machine, a Dell OptiPlex 3020 from 2014: Core i5-4590, 16 GB, Windows 10 Pro, TPM 1.2.
They planned to set it up as a home server and, as its first job, move the Hyper-V test VM `SpeedTest-Win11`
there and run "the things needed to publish to winget". A planning session on 2026-10-05 checked that plan
against the facts:

- The machine cannot run Windows 11 in a supported way. The i5-4590 is not on Microsoft's list of supported
  Intel processors (Verified: Microsoft Learn, "Windows 11 supported Intel processors", read 2026-10-05). The
  OptiPlex 3020 ships a TPM 1.2 and Dell offers no TPM 2.0 firmware for it (Verified: Dell OptiPlex 3020
  datasheet; Dell knowledge base article 000132583 lists the 3040 as the oldest eligible model, read
  2026-10-05). Microsoft's page on installing Windows 11 on devices below the requirements no longer documents
  a registry bypass and says such a PC "won't be entitled to receive updates" (Verified: support.microsoft.com,
  read 2026-10-05; that the bypass text disappeared in December 2024 comes from press reports: Inferred).
- Building needs no machine. CI compiles and packages the MSIX with the .NET SDK on GitHub's Windows runners
  (Verified, S1). Visual Studio adds nothing: it is an editor over the same SDK, and it signs only with a
  certificate, by default a test certificate that every target PC would have to trust by hand (Verified, S2).
- Signing needs a certificate, not a machine. The only certificate compatible with `docs/SAFETY-CONTRACT.md`
  and ADR-0008 is Microsoft's own, applied by the Store after a browser upload (Verified, S7). Microsoft's
  WinGet road for Command Palette extensions leaves MSIX for an Inno Setup `.exe` that installs per user without
  elevation and writes the COM class under `HKEY_CURRENT_USER` (Verified, S5, S6); it drops the package manifest
  the contract's capability rule checks, needs an ADR for the registry writes, and still leaves the `.exe`
  unsealed unless a certificate is bought. A bought certificate held as a CI secret is P0 under the Code Review
  Rules (S11). `docs/PUBLISHING.md` lays this out for the owner.
- A self-hosted GitHub Actions runner on this public repository is unsafe: GitHub's documentation says
  self-hosted runners "should almost never be used for public repositories" (Verified, S10).
- The server would therefore only replace the Hyper-V VM for manual test runs. A Windows 11 guest needs a TPM
  device and UEFI firmware, and Proxmox VE provides a virtual TPM 2.0 and OVMF with Microsoft's Secure Boot keys
  (Verified: Proxmox VE administration guide, chapter "Qemu/KVM Virtual Machines", read 2026-10-05). Microsoft
  also requires the VM host's processor to meet the Windows 11 requirements (Verified: Microsoft Learn,
  "Windows 11 requirements", read 2026-10-05); what Windows setup does inside a VM on this host is Unknown.
  Nothing is gained for publishing either way.

## Decision
1. The Hyper-V VM on the owner's laptop remains the proof surface (`docs/TESTING.md`, the capture harness from
   session 5). Nothing in the test workflow changes.
2. No self-hosted runner, now or later, while the repository is public.
3. No build or signing machine. Publishing, if the owner decides on it (plan item 4.4), goes through the Store:
   the owner uploads the CI-built MSIX, Microsoft signs it, `winget` finds it through the Store source
   (Verified, S8).
4. The OptiPlex is set up, if at all, for the owner's other projects and documented in its own repository.

## Alternatives rejected
- Proxmox on the OptiPlex with a Windows 11 VM as the test surface. Likely works (see Context, last bullet),
  gains nothing for publishing, costs an afternoon of physical access and a second thing to maintain. Open for
  later; a VM there would be a drop-in replacement for the Hyper-V VM.
- Windows 11 bare metal on the OptiPlex. Unsupported CPU and TPM 1.2; no documented bypass; Microsoft disclaims
  updates (Verified, see Context, first bullet).
- Visual Studio on a VM "to build and sign". Visual Studio signs with a certificate the owner would still have
  to obtain (Verified, S2); the certificate question is the whole question.
- The WinGet road with the per-user installer. Not rejected on rights, since it asks for none; not chosen
  because it leaves MSIX, needs an ADR for registry writes, and still needs a seal. The Store does the same job
  without any of that.

## Consequences
- `docs/PLAN.md` item 4.4 stays deferred and points at `docs/PUBLISHING.md`.
- If the owner later sets up the server, revisit point 1 of this ADR; points 2 and 3 stand regardless.
- Documents that described the WinGet road as "administrator rights, Program Files" (plan item 4.4 from
  2026-09-28, the first version of `docs/PUBLISHING.md` and of this ADR) were corrected on 2026-10-07.
