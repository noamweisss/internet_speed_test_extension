# ADR-0017: A text bar meter in fixed content blocks, eased between measurements

Status: accepted · Date: 2026-09-29 · Supersedes ADR-0013 (and ADR-0006's bar; ADR-0011's gauge is not restored)

## Context
The session 4 meter (ADR-0011) drew each speed as an SVG arc in a `data:` image inside one markdown block,
regenerated on every progress report. The owner's VM run on 2026-09-29 showed that it renders, and the owner's
verdict was that its layout and animation are "not very good" (plan item 5.1). ADR-0013 took the gauge out of the
code for `v0.1.1` and said 5.1 would restore it from `6034b22` and record the redrawn meter in the ADR that
supersedes ADR-0013. This is that ADR.

The rendering research of session 5 (`docs/CMDPAL-RENDERING.md`) explains the verdict from the PowerToys source
(§14): nothing in the host interpolates, so the arc jumped every 200 ms; every `Body` set rebuilds the whole
markdown block and recreates each image empty before it loads again, so every update has a blank interval (structure
Verified, visibility Inferred); a `data:` image takes no size hints, is capped at 256 DIP wide and is rasterised at
a DPI-scaled width, so its size depends on the monitor's scaling; the readouts were H3, which the host draws at
12 px; and one block held the whole page, so the heading and connection line were re-parsed with every sample.
§15 ranks the options: easing in the extension (1), one block per moving part (2) and a text meter (3) add no
capability and together are the cheapest path to a meter that is smooth and eased; an SVG kept in its own block at
2 to 4 Hz (4) still has the blank frame and the DPI-dependent size.

The owner asked for two or three visual designs, each fully built so that choosing one is only a pull request.
All three share a base branch with options 1 and 2 and differ in how a speed is drawn.

## Decision
1. **Four fixed content blocks** (§15 option 2). `MeterPage` returns the same four `MarkdownContent` instances
   every time: header (title, status, latency), download meter, upload meter, footer (connection line). The host
   rebuilds only a block whose text changed. `RaiseItemsChanged` is never called on the meter path.
2. **Easing in the extension, maths in Core** (§15 option 1). A one-shot `System.Threading.Timer` in `MeterPage`,
   re-armed every `MeterEasing.TickMilliseconds` (100 ms), moves the shown value toward the last measurement
   through `MeterEasing.Next`, which returns an immutable `MeterFrame(Shown, Scale)` (exponential approach, time
   constant 250 ms, no overshoot; the last remainder snaps once it is invisible on the bar). A meter whose phase is
   not live shows its value exactly. The readout and the bar both show the eased value while a phase runs; the
   final value is exact. A meter block is set when its frame changes (record equality) or a new snapshot arrives (the active marker
   depends on the phase). 100 ms is enough
   here: a text block has no image stage (§5 step 5), and it is the floor §15 gives.
3. **A scale that never shrinks during a run.** `MeterEasing.Next` picks the scale from the measured value, not the
   eased one, through `SpeedFormatter.ScaleFor(mbps, atLeast)`, and it only grows, so the bar does not drop back at
   10, 25, 50, 100 Mbps and so on. A meter with no value yet gets `MeterFrame.Empty` (no scale), which resets it
   for every run.
4. **H2 readouts.** The values under Latency, Download and Upload are H2 (20 px semi-bold), not H3 (§3, §16).
5. **A text bar in a fenced code block** (§15 option 3). Each meter block is the title (with the active marker),
   the H2 readout, and a fenced code block of two lines, each exactly `SpeedFormatter.BarCells` (24) cells wide:
   the bar, and under it a scale line with `0` at the left and the scale's end right-aligned (`250 Mbps`,
   `2.5 Gbps`), so the reader sees what a full bar means. The host draws fenced code in Consolas, whose glyphs
   share one width, inside a card-coloured border; inline code is 10 px and not monospace (§3). The bar fills in
   half-cell steps, 48 over the 24 cells, rounding down: full cells `█`, one `▌` when the remainder is at least
   half a cell, `░` for the rest. It uses only those three glyphs because Consolas and Segoe UI Variable contain
   only `▀ ▄ █ ▌ ▐ ░ ▒ ▓` from the Block Elements range; the eighth blocks `▉▊▋▍▎▏` (and ADR-0006's `▰▱`) fall back
   to Segoe UI Symbol, narrower and shorter, and break the grid (DirectWrite cmap and `MapCharacters` check on the
   owner's laptop, 2026-09-29, session 5 research). The line length is constant, so nothing re-wraps as the value
   moves. `SpeedFormatter.Bar` draws the bar, `SpeedFormatter.BarScale` the scale line; there is no other bar.

The SVG gauge of ADR-0011 is not restored, and `GaugeSvg` stays out of the code. Reasons: the §14 diagnosis; the
§15 ranking, where a text meter is the cheapest smooth and eased option and needs no new capability; the blank
frame on every rebuild of a markdown image, which options 1 and 2 do not remove; the 256-DIP cap and the
DPI-dependent rasterisation of `data:` images. (A restored `GaugeSvg` needs no R6 exemption: the SVG root carries no
`xmlns`, which the session 5 research and the VM runs verified renders fine, so R14 forces no separate rules pull request.)

Two alternatives were built on the same base, on their own branches, for the owner to compare on the VM:

- **B, SVG arc gauge** (`feat/meter-arc-gauge`): `GaugeSvg` restored from `6034b22` with a numeric root size and a
  `viewBox` and no `xmlns`, in its own block, ticking at 250 ms. It keeps the blank frame and DPI-dependent size
  of §14.
- **C, SVG horizontal bar** (`feat/meter-svg-bars`): a bar drawn by `GaugeSvg`, same block and tick, same
  constraints as B.

Chosen by the owner on 2026-09-29 after the VM comparison: it never blinks, follows the theme, fits above the
fold at 800×480, and needs one pull request.

## Consequences
- No image in the meter, so nothing depends on PowerToys 0.95 or later (`data:` images) or on image loading: the
  meter works on any host that renders markdown with fenced code blocks.
- Every `docs/SAFETY-CONTRACT.md` §3 answer stays "No": no host, file, dependency, capability or guard changes; the
  timer and the formatting are in-process.
- `GaugeSvg` stays out of the code; R6 keeps no exception.
- ADR-0006's `▰▱` bar is replaced by this one. ADR-0013's plan to restore the gauge in 5.1 is replaced by this
  decision.
- The scale steps up while a speed rises (10, 25, 50, 100, 250, 500 Mbps), and the bar shortens at each step:
  the VM run saw it three times on a 200 Mbps line. A follow-up may start a run at the previous run's scale.
- The two alternatives stay on GitHub as backlog branches, `feat/meter-arc-gauge` (B) and `feat/meter-svg-bars`
  (C), built, reviewed and run in the test VM on 2026-09-29. B, the dial: 2 then 6 blank frames per download, with
  a layout jump, and the dial's bottom clipped at 480 px. C, the drawn bar: about one blank frame in four, no jump.
  They are to be revisited when the host changes how it renders extension content (PR #50211, in-place Adaptive
  Card updates, milestone 0.102; PR #50443, graph content), plan item 5.3. Each carries its own ADR-0017 draft,
  to be renumbered when picked up.
- What only Windows can show: whether the Consolas grid holds in the host at every scaling, and how the 100 ms
  tick looks on the owner's laptop (`docs/CMDPAL-RENDERING.md` §16).

## Verified in the test VM
Run on 2026-09-29 on `SpeedTest-Win11` (PowerToys 0.101.2652, Command Palette 0.12.12651, 1920×1080 at 100 %),
build `f623fc5`: 161 frames captured at 150 ms; phases latency at frame 3, download 11, upload 64, complete 116
(17.9 s); no blank or collapsed meter frame. At 800×480 the title, status, latency, download value and bar box are
visible without scrolling; upload is below the fold. The code box shows about 40 px of empty space under the scale
line; the markdown has no blank line in the fence, so that space is the host's padding. The 200 % scaling of the
owner's laptop is not covered.

A second run after the fix round (2026-09-29, same VM, build `870e8b4`, 160 frames): latency at frame 2, download 8,
upload 59, complete 111 (17.2 s); again no blank frame. The page icon is back. The speed held at about 125 Mbps, so
the bar stood still while the number changed, and the scale stepped once, 100 to 250 Mbps. The empty band under the
scale line is still there, 41 px of the 84 px code box. The markdown is exactly two lines inside the fence, so it
comes from the host's code-block rendering; a follow-up could look at the toolkit's `MyCodeBlock` for a trailing
line.
