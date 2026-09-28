# Internet Speed Test for PowerToys Command Palette

A [PowerToys Command Palette](https://learn.microsoft.com/windows/powertoys/command-palette/overview) extension that
measures download, upload, and latency from inside the palette, modelled on the
[Raycast Speedtest extension](https://github.com/raycast/extensions/tree/main/extensions/speedtest).

- **Meter view**: a dashboard that follows the running test (latency → download → upload).
- **Details view**: every measured value as a list, one keystroke away (`Ctrl+L`), each copyable.
- **Configurable default view** in the extension's settings.
- Measures against Cloudflare's public speed-test endpoints with plain HTTPS. No third-party executables, nothing
  downloaded at runtime, no accounts (see `docs/decisions/ADR-0002-cloudflare-backend.md`).

## Status

Usable, early. v0.1.0 measures latency, jitter, download and upload and shows the connection details. See
`docs/PLAN.md` for the roadmap and `CHANGELOG.md` for what changed.

## Install

Download the newest release's `.zip` from the repository's **Releases** page and follow `docs/INSTALL.md`
(no Visual Studio, no certificate; Windows Sandbox recommended). Builds of unreleased commits are the
`internet-speed-test-extension-x64-<commit>` artifacts of the CI workflow.

## Development

Read `AGENTS.md` first (it applies to humans too). Then `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`,
`docs/TESTING.md`, `docs/SECURITY.md`. Run `scripts/setup.sh` once after cloning to install the git hooks.

## Authorship

Every line of code and documentation here was written by AI coding agents. The human owner, Noam, had the idea,
set the requirements, and made the product decisions (`docs/decisions/`), and did not write the code.
Reviewers should read the project with that in mind: `docs/SESSION-LOG.md` and git history show which session
produced what.

## License

MIT with an authorship notice, see `LICENSE`. Use it however you like; just keep the notice.
