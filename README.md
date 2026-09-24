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

Pre-release. See `docs/PLAN.md` for the roadmap and `CHANGELOG.md` for what exists.

## Install

Not yet packaged. `docs/PLAN.md` says when a downloadable build arrives; until then, build with Visual Studio 2022
(Windows App SDK workload) using **Build > Deploy**, then run **Reload** in Command Palette.

## Development

Read `AGENTS.md` first (it applies to humans too). Then `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`,
`docs/TESTING.md`, `docs/SECURITY.md`. Run `scripts/setup.sh` once after cloning to install the git hooks.

## License

MIT, see `LICENSE`.
