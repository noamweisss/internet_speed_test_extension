# ADR-0017: The meter is an arc gauge again, in its own block, eased at 4 Hz

Status: accepted · Date: 2026-09-29 · Supersedes ADR-0013 and, with it, ADR-0006 (restores the gauge of ADR-0011, with fixes)

## Context
ADR-0011 drew each speed as an SVG speedometer arc in the meter's markdown. The owner's VM run on 2026-09-29
showed that it renders, and the owner found the layout and the animation "not very good" (plan item 5.1).
ADR-0013 took the gauge out of the code for `v0.1.1` and said that 5.1 restores it from `6034b22`.

The session 5 research (`docs/CMDPAL-RENDERING.md` §14) found five causes, none in the arc geometry:

1. Discrete steps: the arc moved only when a measurement arrived, every 200 ms; nothing in the host
   interpolates.
2. A blank interval per update: every `Body` set rebuilds the whole markdown block and recreates the image empty
   before it is filled asynchronously (§5 step 5; structure Verified, visibility Inferred).
3. Size rules: a `data:` image gets no size hints, is capped at 256 DIP, and is sized only by the root's numeric
   `width` and `height`; the session 4 gauge asked for 220 × 121 (§4).
4. Small readouts: the value was an H3, which the host renders at 12 px in normal weight (§3).
5. One block: the heading, status, latency, both meters and the ISP line were re-parsed five times a second.

The owner asked for two or three visual designs, each fully built, so that choosing one is only a pull request.
All three share a base branch (`feat/meter-live-blocks`) that fixes causes 1, 4 and 5 for any design, and
each adds its own meter on top.

## Decision
The meter view is built like this:

- Four fixed content blocks: header (title, status, latency), download meter, upload meter, footer (connection
  line). `MeterPage` returns the same four instances every time, so a sample rebuilds only its own meter
  (§15 option 2).
- Easing in the extension, maths in Core: a one-shot ticker in `MeterPage` moves each meter toward the last
  measurement with `MeterEasing.Step`, an exponential approach that never overshoots and snaps to the exact
  value when the remainder is below what the readout shows, or when the meter's phase ends (§15 option 1).
- A scale that never shrinks during a run: it follows the measured value, not the eased one, through
  `SpeedFormatter.ScaleFor(mbps, atLeast)`, and resets when a new run starts.
- H2 readouts (20 px semi-bold) under Latency, Download and Upload.
- Each speed is a dial (`GaugeSvg`) in its meter's block, as plain markdown with an empty alt text
  (`![](data:image/svg+xml;base64,…)`), with the title above it and the H2 readout below it. Everything is
  left-aligned: centring needs `<p align="center">` (§3), which would centre the dial and leave the readout on
  the left, so the design keeps one clean column on the left edge.
- The ticker runs at 250 ms (4 Hz, the top of §15 option 4's range for an image), because each rebuild blanks
  the image; the easing time constant is 500 ms, so a frame covers about 39 % of the remaining distance.
- The SVG root is `<svg viewBox='0 0 200 110' width='200' height='110'>`: numeric size under the 256 DIP cap,
  and a viewBox of the same size.
- The root has no `xmlns` attribute. The host does not need it: its SVG sniff looks for `<svg` and its size
  probe reads the root's `width` and `height` with or without a namespace (PowerToys `ImageSourceFactory.cs:87–102, 137`,
  Verified in source); Direct2D's `CreateSvgDocument` draws an SVG with no namespace and even with a wrong one
  (Verified, rendered on the owner's laptop on 2026-09-29); and Microsoft's own Performance Monitor extension
  emits its chart SVG without one (`ChartHelper.cs`). Without the namespace string, the C# holds no `http://`, so
  rule R6 keeps no exception and, under R14 (ADR-0016), no separate rules pull request is needed. This departs
  from ADR-0013, which planned to re-add the R6 exemption of `6034b22`.
- Geometry as in ADR-0011: a semicircle track and a progress arc, centre (100, 100), radius 80, stroke width 14,
  round caps, a sweep of at most a semicircle (large-arc flag 0). Five tick marks outside the track at 0, 25,
  50, 75 and 100 % (radius 90 to 98, stroke width 2) make it read as a speedometer.
- Colours: track `#808080` at `stroke-opacity` 0.35, ticks `#808080` at 0.5, progress `#0078D4` (the Windows
  default accent; the host does not tell an extension the user's accent or theme, §3). No 8-digit hex colours:
  Direct2D draws them black (Verified on the owner's laptop). No text, filters or gradients; only `svg`,
  `path` and `line` with `stroke`, `stroke-width`, `stroke-opacity`, `stroke-linecap` and `fill` (§4).
- `SpeedFormatter.Bar`, the text bar of ADR-0006, lost its only caller and is removed with its tests.

Two other designs were built on the same base for the comparison: A, a text bar of `█ ▌ ░` in a fenced code
block with a 100 ms tick and no image (`feat/meter-text-bars`); C, an SVG horizontal bar under the same
constraints as this design (`feat/meter-svg-bars`). Chosen by the owner on <date> after the VM comparison.

## Consequences
- The dials need PowerToys 0.95 or newer for `data:` images. An older host shows nothing where the dial is
  (the alt text is empty); the readout under it still shows.
- The blank frame per rebuild remains; whether it is visible at 4 Hz is Inferred until the VM run (§16 item 1).
- On a display scaled to 200 % (the owner's laptop) the host rasterises the image's width at scale and its
  height not (§4 rule 3). The VM runs at 100 %, so its captures cannot show that.
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": the `data:` URI is content the extension generates, not
  something it fetches; no new host, file access, dependency or capability. R6 keeps no exception.
- ADR-0011 and ADR-0013 stay as written (ADRs are not edited after acceptance, `docs/CONVENTIONS.md`).
