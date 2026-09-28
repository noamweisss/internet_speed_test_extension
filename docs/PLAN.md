# Plan

Roadmap by agent session. A "session" is one Claude Code conversation, stopped at roughly 60–70 % of its context.
Status: `todo` · `doing` · `done` · `verified` (done and confirmed on a real Windows machine) · `dropped`.

## Session 1 — foundations and a compiling, tested core (this session)

| # | Item | Status |
|---|------|--------|
| 1.1 | Guard hooks: git hooks + Claude Code hooks + `scripts/check.sh` | done |
| 1.2 | Documentation scaffolding: AGENTS.md, CLAUDE.md pointer, docs/*, ADRs 0001–0006, PR template | done |
| 1.3 | `src/SpeedTest.Core`: result model, Cloudflare measurer, formatting, text meter | done |
| 1.4 | `tests/SpeedTest.Core.Tests`: unit tests with faked network, passing locally | done |
| 1.5 | Extension UI: settings (default view), MeterPage, DetailsPage, shared session, shortcuts | done |
| 1.6 | CI on Windows: build extension, run tests, `check.sh all`; green on the feature branch | done |
| 1.7 | Hand-off: SESSION-LOG, CHANGELOG written; PR opened when the owner asks | done |

Expected state at the end of session 1: the code compiles in CI and Core is unit-tested, but nothing has run inside
Command Palette on a real PC yet. Treat the UI as unverified.

## Session 2 — first real run and fixes

| # | Item | Status |
|---|------|--------|
| 2.0 | Owner: enable Dependabot alerts and confirm secret scanning (repo Settings → Security); install CodeRabbit; review and merge PR #1 | done |
| 2.1 | CI publishes the built extension as a downloadable artifact plus a PowerShell install script (Developer Mode + `Add-AppxPackage -Register`), so the owner never needs Visual Studio (`docs/INSTALL.md`, with the Windows Sandbox path from `docs/SAFETY-CONTRACT.md` §5 first) | done |
| 2.2 | Owner installs it and runs the manual checklist in `docs/TESTING.md` | done (shortcuts, settings, update-in-use still to check) |
| 2.3 | Fix whatever the real host reveals (API mismatches, layout, shortcuts, timing) | done (PR #7) |
| 2.5 | Fix the connection-info and jitter bugs from the second VM run: `/meta` Referer, `colo` object, percent-encoded header values, latency warm-up probe (`docs/SESSION-LOG.md`, session 2) | todo |
| 2.6 | Merge Dependabot PRs #2, #3, #4, #6 (add their Safety impact notes) | todo |
| 2.4 | Tag `v0.1.0`, GitHub release with the MSIX and install notes (after 2.5 and 2.6) | todo |

Expected state at the end of session 2: a usable v0.1.0 you can install and run daily.

>[!note]Owner Note:
> This did not happen. I stopped Session 2 after merging PR 7 because it became too long and because I took a few days break and wanted to start clean, so I pushed everything up a session.

# Session 3

| # | Item | Status |
|---|------|--------|
| 3.1 | Fix the connection-info and jitter bugs from the second VM run: `/meta` Referer, `colo` object, percent-encoded header values, latency warm-up probe (`docs/SESSION-LOG.md`, session 2) | verified (VM run 2026-09-28: ISP, location, jitter all correct; PR #11) |
| 3.2 | Merge Dependabot PRs #2, #3, #4, #6 (add their Safety impact notes) | done |
| 3.3 | Tag `v0.1.0`, GitHub release with the MSIX and install notes  | verified (published 2026-09-28; owner installed from the Releases page on the laptop and ran it) |

Expected state at the end of session 3: a usable v0.1.0 you can install and run daily.

## Session 4 — polish (only what real use asks for)

| # | Item | Status |
|---|------|--------|
| 4.0 | Independent reviewers on every PR without the hourly wait: Claude review workflow on the owner's subscription, Codex rules in `AGENTS.md`, commit identities (ADR-0012) | done (app and secret added by the owner on 2026-09-28; CodeRabbit uninstall waits for PR #15) |
| 4.1 | Meter view as an image gauge if the markdown renderer supports it (ADR-0006 revisit) | todo |
| 4.2 | Copy full summary; optional "result as markdown" | todo |
| 4.3 | Measurement tuning (parallel streams, durations) against real connections | todo |
| 4.4 | Optional: publish to WinGet / Store (template skill `publish-extension`) | todo |

## Out of scope (unless asked)

Result history, charts over time, server selection, speedtest.net results and share links (ADR-0002), telemetry.
