# Session log

Hand-off notes between agent sessions, newest first. The SessionStart hook prints the top entry; the Stop hook
refuses to end a session that changed the repo without a new entry. Keep entries factual: done, verified, not
verified, blocked, next.

## Session 2 — 2026-09-24 — branch `feat/install-without-visual-studio`

**Done**
- Harness branch `claude/determined-curie-7om0u6` renamed to `feat/install-without-visual-studio` (AGENTS.md §4).
  The generated remote branch still exists (same commit as `main`); not deleted, G6 blocks branch deletion.
- Plan item 2.1: CI builds an unsigned, self-contained MSIX, checks its contents, runs
  `install/Install-SpeedTestExtension.ps1` on the runner (install, verify, uninstall), and uploads the package
  with the script as artifact `internet-speed-test-extension-x64`. `docs/INSTALL.md`, ADR-0008.
- `install/` is safety-sensitive: added to R13 (`scripts/safety-impact.sh`), SAFETY-CONTRACT §2, CodeRabbit guard
  paths; R4 now scans `.ps1`.

- Real bug found by the first package build: MSIX rejects underscores in `Identity Name` (C00CE169), so no
  package could ever have been built, not even by Visual Studio. Identity is now `InternetSpeedTestExtension`
  (ADR-0009); project, assembly, exe name, and CLSID unchanged.

**Owner facts**
- Windows 11 Pro: Windows Sandbox is available, so INSTALL.md path A (Sandbox) applies.
- Sandbox attempt failed twice: Sandbox terminated with 0x80370106 ("virtual machine or container exited
  unexpectedly") while the PowerToys installer hung and Task Manager was opened. Cause unknown. The owner is
  setting up a persistent Hyper-V test VM (INSTALL.md path 2) with a checkpoint and a log-collection script,
  driven by a local Claude session on the laptop. Nothing of the extension was installed yet.

**Verified**
- `scripts/check.sh all` passes locally.
- CI run 36041156689 on commit 305f2cf: all jobs green. On the Windows runner the script installed the package,
  `Get-AppxPackage` found it, and `-Uninstall` removed it. Artifact `internet-speed-test-extension-x64`: 14 MB zip,
  two files (MSIX + script), expires 2026-12-23.

**Not verified**
- The extension has still never run inside Command Palette (the runner has no PowerToys). The Release build is
  trimmed ("Optimizing assemblies for size" in the log); a trimming problem would only show at runtime.
- Whether a folder-registered package keeps loading after Developer Mode is switched off (INSTALL.md says it may not).
- Whether PowerToys Command Palette runs inside Windows Sandbox.

**Next**
- The owner can test now from the artifact of the latest green run on this branch, following `docs/INSTALL.md` (Sandbox) and the `docs/TESTING.md` checklist
  (item 2.2). Then 2.3 fixes, 2.4 release.

## Session 1 — 2026-09-24 — branch `feat/speedtest-core-and-ui`

**Done**
- Guard hooks (git + Claude Code) and `scripts/check.sh` with rules R1–R11, C1, P1–P3, G1–G8, W1–W2, S1.
- Documentation scaffolding, ADRs 0001–0006, roadmap in `docs/PLAN.md`.
- `src/SpeedTest.Core` (measurer, model, formatting, meter markdown) and 51 xUnit tests.
- Extension: `SpeedTestCommandsProvider`, `SettingsManager`, `SpeedTestSession`, `ViewCommands`, `MeterPage`,
  `DetailsPage`. Template types renamed per ADR-0005; `Program.cs` updated once (trailer `Protected-Change`).
- CI workflow `.github/workflows/ci.yml` (rules, Core tests on Linux, extension build on Windows).
- Safety layer for a non-developer owner: `docs/SAFETY-CONTRACT.md`, `scripts/safety-impact.sh` (R13, runs on
  PRs), R12 capability check, `.github/workflows/codeql.yml`, `.github/dependabot.yml`, `.coderabbit.yaml`.

**Verified**
- Hook scripts self-tested with sample inputs (blocked and allowed cases). Claude hooks confirmed live in-session.
- Core tests pass locally on Linux (.NET 10.0.401). `scripts/check.sh` passes.
- Toolkit API signatures taken from the NuGet package (Microsoft.CommandPalette.Extensions 0.9.260303001) via
  reflection, not from memory; sample pages from microsoft/PowerToys read for usage patterns.

- CI run 1 on commit a02a46d: all three jobs green (rules, Core tests on Linux, extension build on Windows x64).

**Not verified**
- Nothing has run inside Command Palette. Unknowns to confirm in session 2: whether `MarkdownContent.Body`
  updates re-render live, whether `RaiseItemsChanged` on a `ContentPage` is needed/allowed, whether shortcut
  key chords fire on `ListItem.MoreCommands`, and whether `GetContent`/`GetItems` are only called on navigation
  (drives the "start if stale" behaviour in `SpeedTestSession`).
- The upload measurement counts bytes as they are handed to the socket, not as acknowledged; may read high on
  buffered links. Tuning item 3.3.

**Owner decisions this session**
- Option A (Cloudflare, ADR-0002). Repo is public. License: MIT plus authorship notice, "Noam" as the human.
- No auto-generated branch names; the harness branch `claude/elegant-hypatia-lfpr8p` was renamed and the
  remote copy deleted with the owner's explicit permission (rule P3, AGENTS.md §4).
- Owner has VS Code, not Visual Studio; session 2 must deliver an install path without Visual Studio.

**Environment notes**
- Cloud environment has .NET 10 SDK via setup script and network access to `speed.cloudflare.com`, NuGet, and
  Microsoft download hosts. Ookla hosts are not allowlisted (Option A chosen, ADR-0002).

**Review round 1**
- CodeRabbit (assertive profile) returned 17 findings on PR #1: 8 security, 4 stability/correctness, 5 docs and
  test quality. Every one was verified against the code and fixed in one commit; none were disputed. Notable:
  redirects were followed by default (fixed), `/meta` failures were fatal (now optional), non-HTTP exceptions
  could leave the UI stuck (now surface as Failed), server text reached markdown unescaped (now escaped).
- Observation for future sessions: the reviewer's "cf-meta-*" header claim conflicted with headers observed by
  curl earlier in the session (bare `city`/`colo`); the code now reads both. The tests previously mirrored the
  code's assumption, so they could not catch it: a reminder that fakes encode beliefs.

**Review round 2 (Codex, posted by the owner using docs/REVIEW-PROMPT.md)**
- 6 findings: 2 blockers (stalled `/meta` body not time-bounded because `HttpClient.Timeout` ends at the headers
  with `ResponseHeadersRead`; download responses not bounded by the requested bytes), 1 major (R10/R12 read the
  working tree in staged mode), 2 minor (Failed never retried on reopen; ownership check and publish not atomic),
  1 nit (`Task.Run` around the transfer loops, kept: it guards the synchronous-completion case the tests exercise
  and keeps the loops off the host's thread). Five fixed in one commit, 69 tests.
- The two reviewers found different things: CodeRabbit the redirect and escaping issues, Codex the timeout and
  byte bounds. Two independent models, same five questions, was worth it.

**Operating notes for reviewers and CI (learned in this session)**
- CodeRabbit does not review automatically on repositories with fewer than 10 stars: after every push, post
  `@coderabbitai review` as a PR comment. The free tier also rate-limits reviews (about one per half hour);
  a rate-limited trigger must be repeated later. Reply on each thread before pushing so it can verify the commit.
- GitHub's "automatic dependency submission" (enabled by the owner under Security) runs `dotnet restore` on
  Linux. The extension project sets `EnableWindowsTargeting` so that restore succeeds off Windows.

**Pull request**
- [noamweisss/internet_speed_test_extension#1](https://github.com/noamweisss/internet_speed_test_extension/pull/1),
  opened at the owner's request at the end of session 1 and **merged into main on 2026-09-24** after two
  independent reviews (CodeRabbit round 1: 17 findings; Codex rounds 2 and 2b: 7 findings) with every finding
  fixed or answered. The owner dismissed CodeRabbit's stale "changes requested" verdict and merged with a merge
  commit. The owner is adding a ruleset on `main` (pull request + status checks required).

**Session 1 final state**
- `main` = session-1 result. 69 Core tests. Nothing has yet run inside Command Palette on a real PC.
- Owner has not yet said which Windows edition they use; ask before writing `docs/INSTALL.md` (Sandbox needs Pro).

**Next**
- Session 2 in `docs/PLAN.md`, starting at item 2.1: CI artifact + install script without Visual Studio, then the
  owner runs the manual checklist. Rename the harness branch first (AGENTS.md §4).
- The safety-impact CI job has only been tested locally (`scripts/safety-impact.sh` with a fake PR body); its
  first real run is on the first PR. CodeQL ran on push; check the Security tab for findings.
