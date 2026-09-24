# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [SemVer](https://semver.org/). The version also lives in `internet_speed_test_extension/Package.appxmanifest`.

## [Unreleased]

### Added
- Install without Visual Studio: CI publishes an unsigned, self-contained MSIX with
  `install/Install-SpeedTestExtension.ps1` as the `internet-speed-test-extension-x64` artifact, and proves on a
  clean Windows runner that the script installs and removes it (`docs/INSTALL.md`, ADR-0008).

### Changed
- The extension project restores on Linux/macOS (`EnableWindowsTargeting`), so GitHub's automatic dependency
  submission and agents in containers can run `dotnet restore`; building still needs Windows.

### Added
- Project rules, guard hooks (git and Claude Code), documentation scaffolding, roadmap, and ADRs 0001–0006.
- `SpeedTest.Core`: Cloudflare-based measurement (connection info, latency, jitter, download, upload), formatting,
  and the text meter renderer, with 51 unit tests.
- Extension: meter dashboard view, detailed results view with copyable rows, `Ctrl+L` to switch, `Ctrl+R` to rerun,
  and a "Default view" setting.
- CI on GitHub Actions: rules check, Core tests on Linux, extension build on Windows (warning-free for project code).
- Review round 2 follow-up (Codex): each download read is capped at the remaining requested bytes, so a
  length-unknown response can never be counted past the request; the test asserts the exact bytes read.
- Review round 2 (Codex via the owner, 6 findings): `/meta` body read has its own deadline (`MetaTimeout`);
  download responses are bounded by the requested byte count and an oversized `Content-Length` is rejected;
  R10/R12 inspect the staged manifest and sources, not the working tree; a failed run is retried when a view is
  reopened after 10 s; run ownership and snapshot updates are atomic.
- Review round 1 (CodeRabbit, 17 findings, all addressed): redirects disabled and buffered responses capped;
  `/meta` optional and size-bounded; connection resets and unexpected errors surface as a failed state instead of
  a stuck one; server-supplied text markdown-escaped; `cf-meta-*` header names read with bare-name fallback;
  progress from superseded runs dropped; workflow actions pinned to commit SHAs; rule fixes (R2 depth, R8 scope,
  NUL-safe file loop, G1–G8 after git global options, R13 requires real text, session marker per worktree); docs
  aligned; ADR-0007 on HTTPS transport metadata.
- `docs/REVIEW-PROMPT.md`: one prompt any reviewer agent can be given, so second opinions are comparable.
- Owner-facing safety contract (`docs/SAFETY-CONTRACT.md`), safety-impact PR check (R13), manifest capability
  check (R12), CodeQL workflow, Dependabot config, and CodeRabbit review instructions.
