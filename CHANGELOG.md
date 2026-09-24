# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [SemVer](https://semver.org/). The version also lives in `internet_speed_test_extension/Package.appxmanifest`.

## [Unreleased]

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
- Review round 1 (CodeRabbit, 17 findings, all addressed): redirects disabled and buffered responses capped;
  `/meta` optional and size-bounded; connection resets and unexpected errors surface as a failed state instead of
  a stuck one; server-supplied text markdown-escaped; `cf-meta-*` header names read with bare-name fallback;
  progress from superseded runs dropped; workflow actions pinned to commit SHAs; rule fixes (R2 depth, R8 scope,
  NUL-safe file loop, G1–G8 after git global options, R13 requires real text, session marker per worktree); docs
  aligned; ADR-0007 on HTTPS transport metadata.
- Owner-facing safety contract (`docs/SAFETY-CONTRACT.md`), safety-impact PR check (R13), manifest capability
  check (R12), CodeQL workflow, Dependabot config, and CodeRabbit review instructions.
