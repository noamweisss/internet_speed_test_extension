# Architecture

## Shape

```
Command Palette host (PowerToys)
   │  COM (out-of-process)                        internet_speed_test_extension/   (Windows, MSIX)
   ▼
SpeedTestExtension ─► SpeedTestCommandsProvider ─► SettingsManager (defaultView)
                                                 ├► MeterPage   (ContentPage, markdown dashboard)
                                                 └► DetailsPage (ListPage, one item per value, copy commands)
                                                       │ both observe one
                                                       ▼
                                             SpeedTestSession (state: idle / running / done / failed, last result)
                                                       │ runs
                                                       ▼
                                             SpeedMeasurer  ──────────────── src/SpeedTest.Core/ (any OS, no UI)
                                             ├ CloudflareEndpoints (URLs, constants)
                                             ├ SpeedTestSnapshot (record: phase, connection, values)
                                             └ Formatting (units, meter, easing)
                                                       │ HTTPS only, hosts in scripts/allowed-hosts.txt
                                                       ▼
                                             speed.cloudflare.com
```

## Why two projects (ADR-0003)

`src/SpeedTest.Core` has no Windows, WinRT, or Command Palette dependency. It compiles and tests on Linux, macOS, and
Windows, which means agents can run its tests in any environment and CI can run them on every push.
The extension project only compiles on Windows; it is deliberately thin so that little of value is untested.

## Measurement (ADR-0002)

Phases, in order, each reporting progress through `IProgress<SpeedTestSnapshot>` and honouring a `CancellationToken`:

1. **Meta**: one request to `/meta` gives ISP, public IP, location, and the Cloudflare data centre serving the
   test. It is optional and size-bounded: on any failure the headers of the first latency probe fill in what they
   can. Shown in the UI, never persisted. Server-supplied text is markdown-escaped before display.
2. **Latency**: N small `GET /__down?bytes=0` requests. Report median latency and jitter (median of the absolute
   differences between consecutive samples, ADR-0010). `server-timing` header is subtracted where present so server processing time is excluded.
3. **Download**: several parallel `GET /__down?bytes=<size>` streams for a fixed duration; throughput is bytes
   received over elapsed time, sampled every ~200 ms for the live meter. Bytes are read and discarded.
4. **Upload**: several parallel `POST /__up` streams of a fixed-size zero-filled body for a fixed duration.
   The payload contains no user data.

Measurement constants (duration, parallelism, sizes, progress interval) live in `SpeedTestOptions` in Core. The
`HttpClient` itself (30 s timeout, redirects disabled, 64 KB buffer cap) is configured in `SpeedTestSession` in
the extension, which owns its lifetime.

## Views

- **Meter** (`ContentPage` + four `MarkdownContent` blocks: heading with status and latency, download, upload,
  connection line): the dashboard. The page returns the same blocks every time, so the host rebuilds only a block
  whose text changed (`docs/CMDPAL-RENDERING.md` §5). While a test runs, a 250 ms ticker in the page moves each
  meter toward the last measurement (`MeterEasing` in Core) and snaps it to the exact value when its phase ends.
  Each speed is an H2 readout over a drawn bar: an SVG image (`GaugeSvg` in Core, a `data:` URI, rounded track,
  blue fill, ticks at the quarters) with a body-text scale line under it (ADR-0017). Shows the phase in progress, the live value, and the final summary. `Ctrl+L` opens Details, `Ctrl+R` reruns, `Ctrl+Shift+C` copies
  the summary (`ResultSummary.PlainText`), `Ctrl+Shift+M` copies it as a markdown table.
- **Details** (`ListPage`): one `ListItem` per value (download, upload, latency, jitter, ISP, IP, location, server,
  test time). Each item's command copies the value. `Ctrl+L` opens Meter, `Ctrl+R` reruns, `Ctrl+Shift+C` copies
  the summary, `Ctrl+Shift+M` copies it as a markdown table. The summary leaves the IP address out; it is copyable
  only from its own row.
- **Default view**: `ChoiceSetSetting("defaultView")` in `SettingsManager`. The top-level command opens the chosen
  page. Both pages share one `SpeedTestSession`, so switching views never restarts a running test.

## Data and privacy

Nothing is written to disk by this code. Command Palette persists only the settings values. Results live in memory
for the extension process lifetime. See `docs/SECURITY.md`.
