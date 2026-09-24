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
                                             └ Formatting (units, text meter)
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
2. **Latency**: N small `GET /__down?bytes=0` requests. Report median latency and jitter (mean absolute deviation of
   consecutive samples). `server-timing` header is subtracted where present so server processing time is excluded.
3. **Download**: several parallel `GET /__down?bytes=<size>` streams for a fixed duration; throughput is bytes
   received over elapsed time, sampled every ~200 ms for the live meter. Bytes are read and discarded.
4. **Upload**: several parallel `POST /__up` streams of a fixed-size zero-filled body for a fixed duration.
   The payload contains no user data.

Measurement constants (duration, parallelism, sizes, progress interval) live in `SpeedTestOptions` in Core. The
`HttpClient` itself (30 s timeout, redirects disabled, 64 KB buffer cap) is configured in `SpeedTestSession` in
the extension, which owns its lifetime.

## Views

- **Meter** (`ContentPage` + `MarkdownContent`): the dashboard. v1 draws the meters as text (Unicode bars) because
  Command Palette markdown has no native gauge and an image-based gauge is unverified (ADR-0006). Shows the phase in
  progress, the live value, and the final summary. `Ctrl+L` opens Details, `Ctrl+R` reruns.
- **Details** (`ListPage`): one `ListItem` per value (download, upload, latency, jitter, ISP, IP, location, server,
  test time). Each item's command copies the value. `Ctrl+L` opens Meter, `Ctrl+R` reruns.
- **Default view**: `ChoiceSetSetting("defaultView")` in `SettingsManager`. The top-level command opens the chosen
  page. Both pages share one `SpeedTestSession`, so switching views never restarts a running test.

## Data and privacy

Nothing is written to disk by this code. Command Palette persists only the settings values. Results live in memory
for the extension process lifetime. See `docs/SECURITY.md`.
