# ADR-0017: Draw each speed as an SVG bar in its own block, eased at 4 Hz

Status: accepted · Date: 2026-09-29 · Supersedes ADR-0013 (and replaces the arc of ADR-0011 with a bar)

## Context
ADR-0011 drew each speed as an SVG speedometer arc inside the one markdown block of the meter page. The owner's VM
run on 2026-09-29 showed that it renders, and their verdict was that the layout and the animation are "not very
good"; ADR-0013 took the gauge out of `v0.1.1` until plan item 5.1 redraws it. The rendering research
(`docs/CMDPAL-RENDERING.md` §14) explains what the arc ran into, none of it in the geometry: the arc jumped once
per measurement because nothing in the host interpolates; every `Body` set rebuilt the whole block and recreated
the image empty; a `data:` image gets no size hints, is capped at 256 DIP, and is rasterised at the monitor's
scale in width but not in height; the readouts were H3, which the host draws at 12 px; and the heading, status,
latency and connection line were re-parsed five times a second for one arc.

The owner asked for two or three designs, each fully built, so that choosing one is only a pull request. All three
share a base branch that carries `docs/CMDPAL-RENDERING.md` §15 options 1 and 2.

## Decision
1. **Four fixed blocks** (§15 option 2). `MeterPage` returns the same four `MarkdownContent` instances every time:
   header (title, status, latency), download meter, upload meter, footer (connection line). A sample rebuilds only
   its own meter's block.
2. **Easing in the extension, maths in Core** (§15 option 1). A one-shot timer in `MeterPage`, re-armed after each
   tick, moves the shown value toward the last measurement through `MeterEasing.Step` (exponential approach, never
   overshoots, snaps when the remainder is below what the readout shows). A meter whose phase is not live shows its
   value exactly.
3. **A scale that never shrinks during a run.** `SpeedFormatter.ScaleFor(mbps, atLeast)` grows with the measured
   value and never drops back, so the eased bar grows into its scale instead of jumping back at 10, 25, 50 Mbps and
   so on.
4. **Readouts as H2** (20 px semi-bold), never H3 (§3, §16).
5. **The bar is an SVG image in the meter's own block, at 4 Hz** (§15 option 4, with a bar instead of an arc).
   `GaugeSvg` in Core (the name ADR-0013 reserved for the returning gauge) renders a horizontal linear gauge: a
   rounded track, a rounded fill, and three thin tick marks at 25, 50 and 75 %, drawn over the fill. The meter
   block is the title, the H2 readout, the image as a base64 `data:` URI on its own paragraph, and one body-text
   line with the scale (`scale 250 Mbps`), so a scale step is understandable. `MeterEasing.TickMilliseconds` is
   250 ms: an image is recreated empty on every rebuild (§5 step 5), so fewer rebuilds mean fewer blank frames.
   The time constant is 500 ms, twice the tick: each tick covers 39 % of the remaining distance, small enough
   steps to read as a glide at 4 Hz.
6. **Root size and viewBox.** `<svg viewBox='0 0 240 20' width='240' height='20'>`: numeric width and height, at
   most 256 (the `data:` image cap, §4), and a viewBox, because without numeric sizes the host's size probe
   overflows (§4 rule 2).
7. **No `xmlns`.** Verified in session 5: the host's SVG sniff and size probe ignore the namespace
   (`ImageSourceFactory.cs:87–102, 137`); Direct2D `CreateSvgDocument` draws an SVG with no namespace, and even
   with a wrong one (rendered on the owner's laptop, 2026-09-29); Microsoft's own Performance Monitor `ChartHelper`
   emits SVG without one. Without the namespace string, rule R6 (no `http://` in C#) needs no exemption, so under
   R14 (ADR-0016) there is no separate rules pull request. The cost: the SVG is not a valid standalone file for a
   browser, which the extension never needs.
8. **Colours and the subset.** Only §4's subset: `svg rect line`, attributes `x y width height rx fill
   fill-opacity stroke stroke-width stroke-opacity` and the `x1 y1 x2 y2` a line needs. Track `#808080` at
   `fill-opacity` 0.35 and ticks `#808080` at `stroke-opacity` 0.5 read on both themes; the fill is the Windows
   default accent `#0078D4`, because an image cannot follow the host theme or accent. Eight-digit hex colours draw
   black in Direct2D (Verified, session 5), so transparency is an opacity attribute. The numbers stay markdown
   text, which follows the theme.

**Alternatives, built on other branches for the owner's comparison:**
- **A, text bar** (`feat/meter-text-bars`): a 24-cell bar of `█ ▌ ░` in a fenced code block with a scale line,
  100 ms tick, no image. No blank frame and fully testable, but Consolas lacks the eighth blocks, so a cell can
  only be full, half or empty, and a fallback glyph breaks the monospace grid.
- **B, arc gauge** (`feat/meter-arc-gauge`): the session 4 arc restored from `6034b22` with the same fixes
  (numeric root size, a viewBox, tick marks, no `xmlns`, its own block, 250 ms tick).

What C offers over them: a drawn bar with a smooth fill at any value (A's cells cannot show less than half a
cell), a sixth of the arc's height (20 DIP against 121), and the same left edge as the text around it. Why the owner picked C:
<owner's reason>; chosen by the owner on <date> after the VM comparison.

## Consequences
- Needs PowerToys 0.95 or newer for `data:` images. An older host shows nothing where the bar is; the readout and
  the scale line still show.
- The blank frame per rebuild (§5 step 5) is Inferred until the VM run: whether a 20-DIP image that is recreated
  four times a second blinks is not known yet.
- On a 200 % display (the owner's laptop) the width is rasterised at scale and the height is not (§4 rule 3). The
  VM runs at 100 %, so it cannot show this; only the laptop can.
- The text bar `SpeedFormatter.Bar` loses its only caller and is deleted with its tests.
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": the data URI is generated in the process, not fetched; no
  file, no host, no dependency, no capability. Rule R6 keeps no exception.
- ADR-0013 and ADR-0011 stay as written (ADRs are not edited after acceptance).
