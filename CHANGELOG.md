# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [SemVer](https://semver.org/). The version also lives in `internet_speed_test_extension/Package.appxmanifest`.

## [Unreleased]

### Added
- Project rules, guard hooks (git and Claude Code), documentation scaffolding, roadmap, and ADRs 0001–0006.
- `SpeedTest.Core`: Cloudflare-based measurement (connection info, latency, jitter, download, upload), formatting,
  and the text meter renderer, with 51 unit tests.
- Extension: meter dashboard view, detailed results view with copyable rows, `Ctrl+L` to switch, `Ctrl+R` to rerun,
  and a "Default view" setting.
- CI on GitHub Actions: rules check, Core tests on Linux, extension build on Windows.
