# Changelog

All notable changes to this project are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [SemVer](https://semver.org/). The version also lives in `internet_speed_test_extension/Package.appxmanifest`.

## [Unreleased]

### Changed
- Download and upload are drawn bars (a rounded track, a blue fill, ticks at the quarters) with the scale
  written under each bar. They need PowerToys 0.95 or newer; an older version shows the numbers without the bars.
- The meter moves smoothly between measurements instead of jumping, and the numbers under Latency, Download and
  Upload are bigger. A bar keeps its scale for the whole test, so it no longer drops back when the speed crosses
  10, 25, 50, 100 Mbps and so on. The meter view redraws only the section that changed.

## [0.1.1] - 2026-09-29

The session 4 measurement fixes and the copy commands, without the SVG gauge: it rendered, the owner did not like
its layout yet, so this version keeps the text bar of v0.1.0 (ADR-0006, ADR-0013) and the gauge waits for plan item
5.1.
Install it over v0.1.0 with the same command; the old version is removed first.

### Added
- `Ctrl+Shift+C` copies a summary of the result (download, upload, latency and jitter, ISP, location, server, time)
  as plain text; `Ctrl+Shift+M` copies it as a markdown table. Both work in the meter and the details view. The
  summary leaves out the IP address, which stays copyable on its own row in the details view. Control characters
  in server-supplied text (a line break inside an ISP name) and in the status heading become spaces, so the
  summary is always its seven lines (Codex and CodeRabbit reviews).

### Changed
- Rule R6 (no plain `http://` in C#) now scans single-quoted strings too. The exemption for the SVG namespace name
  in `GaugeSvg.cs` (ADR-0011) was removed together with the gauge (ADR-0013); the rule has no exception again.
- Jitter is the median, not the mean, of the differences between consecutive latency samples, and the test takes
  20 samples instead of 10 (ADR-0010). On the owner's laptop the extension process gets a few isolated slow
  samples that a plain console process does not; they made the mean jitter larger than the latency. The median
  ignores an isolated spike. Each test sends ten more zero-byte requests.
- Server processing time is now subtracted from every latency sample in full: all `Server-Timing` durations are
  summed, not only the first (real probe answers carry `cfSpeedEdge` and `cfSpeedWorker` on one header). Only a
  real `dur` parameter counts, never text inside a quoted description (escaped quotes included), and each value
  and the sum are bounded at 60 s (CodeRabbit review).

## [0.1.0] - 2026-09-28

First usable version: install it from the GitHub release, run it daily. Tested by the owner in a Windows 11 VM.

### Added
- Install without Visual Studio: CI publishes an unsigned, self-contained MSIX with
  `install/Install-SpeedTestExtension.ps1` as the `internet-speed-test-extension-x64-<commit>` artifact, and proves on a
  clean Windows runner that the script installs and removes it (`docs/INSTALL.md`, ADR-0008). Before it removes an
  installed version, the script checks the new package's size and file count, unpacks it into a separate
  folder, and checks its package name there, so a broken or foreign download leaves the current install untouched.
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

### Changed
- Latency probes stop at the response headers and never read a body, so latency is the time to the first
  response byte. A probe answer that carries a body is dropped unread instead of buffered, so the network cannot
  make the extension hold more than it asked for.
- The meter view lists Latency first, then Download and Upload, the order the test measures them in, and marks
  Latency as active while it is measured.
- The extension project restores on Linux/macOS (`EnableWindowsTargeting`), so GitHub's automatic dependency
  submission and agents in containers can run `dotnet restore`; building still needs Windows.

### Fixed
- The ISP is shown again. Cloudflare's `/meta` endpoint answers `403 {}` unless the request names
  `https://speed.cloudflare.com/` as its Referer; the request now does. Same host, no user data in the header.
- Connection details read `/meta` correctly: the serving data centre arrives as an object (`"colo": {"iata": ...}`),
  not a string, so every `/meta` answer used to be discarded and the details fell back to the probe headers,
  which carry no ISP.
- The location reads "H̱olon, IL" instead of "H%CC%B1olon, IL": percent-encoded header values are decoded
  (invalid escapes are kept as they are; values over 256 characters are ignored).
- Jitter is no longer larger than latency on a cold connection: one unmeasured warm-up probe opens the connection
  before the latency samples, so the DNS + TCP + TLS setup cost does not count as a sample. Each test now sends
  one more zero-byte request.
- The meter view now updates live during a test. Before, it froze on "Measuring latency" and showed results only
  when reopened: redrawing asked Command Palette to reload the page, and the reload redrew again, in a loop that
  also stalled the test.
- Without a working network the test now fails within about 10 s instead of up to 35 s: connecting to the server
  has its own 5 s limit.
- Command Palette no longer lists a second, icon-less "Internet Speed Test" entry that did nothing: the package's
  app is hidden from app lists (`AppListEntry="none"`); the extension itself is unaffected.
- The MSIX package identity is now `InternetSpeedTestExtension`: the template name with underscores is invalid for
  a Windows package, so no package could be built (ADR-0009).

[Unreleased]: https://github.com/noamweisss/internet_speed_test_extension/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/noamweisss/internet_speed_test_extension/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/noamweisss/internet_speed_test_extension/releases/tag/v0.1.0
