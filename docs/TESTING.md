# Testing

## Layers

| Layer | Where | Runs on | How |
|-------|-------|---------|-----|
| Unit tests | `tests/SpeedTest.Core.Tests` (xUnit) | Any OS, every push | `dotnet test tests/SpeedTest.Core.Tests` |
| Rules | `scripts/check.sh` | Any OS, every commit and push | pre-commit hook; `scripts/check.sh all` in CI |
| Extension build, package, install script | `.github/workflows/ci.yml` | Windows runner, every push | builds the MSIX, runs `install/Install-SpeedTestExtension.ps1` (install, uninstall) |
| Manual test | Command Palette on a Windows PC | Before a release | checklist below |

## Unit test rules

- Tests fake the network with a `HttpMessageHandler` stub. No test touches the internet.
- Name: `Method_Scenario_Expectation`. One assertion topic per test.
- Every bug fix adds the test that would have caught it.
- Cover: unit formatting, latency/jitter maths, throughput calculation, phase ordering, cancellation, timeout and
  size bounds, text meter rendering, settings parsing (default when value missing or invalid).

## Manual checklist (Windows)

1. Install the CI build (`docs/INSTALL.md`, Windows Sandbox first). Run **Reload** in Command Palette.
2. Open "Internet Speed Test". The default view from settings opens and the test starts.
3. Watch the meter progress through latency → download → upload; values are plausible for your connection.
4. `Ctrl+L` switches view without restarting; `Ctrl+R` restarts. Esc leaves the page; the test keeps running in
   the background (owner decision, session 2) and reopening the page shows it.
5. Details: Enter on an item copies its value (toast appears).
6. Disconnect the network and rerun: a clear error, no crash, no hang beyond the timeout.
7. Change the default view in settings, reopen: the other view is the default.

Record the outcome in `docs/SESSION-LOG.md` and the PR body.
