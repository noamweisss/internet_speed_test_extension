# ADR-0020: The meter bar in eighth-cell steps, in a monospace plain-text block

Status: proposed (accepted when the owner merges it after the VM comparison) · Date: 2026-10-07 · Supersedes
decision 5 of ADR-0017 (the bar in fenced code); decisions 1 to 4 of ADR-0017 stay

## Context
ADR-0017's bar fills 24 cells in half-cell steps, 48 steps in all, because it sits in a fenced code block and the
host draws fenced code in Consolas, which has only `█ ▌ ░` (and `▀ ▄ ▐ ▒ ▓`) from Block Elements. The owner, who
uses v0.2.0 daily, asked whether a font made for progress bars could be shipped with the extension to make the
meter look better.

Such fonts exist: Fira Code has dedicated progress-bar glyphs (U+EE00 to U+EE0B, private use area), and Sparks
(After the Flood) draws bar charts from numbers through OpenType contextual alternates. None of them can be used
here. The research of 2026-10-07 (`docs/CMDPAL-RENDERING.md` §8, "Fonts") found:

- An extension cannot name a font. Markdown has no font control; `IPlainTextContent.FontFamily` is an enum with
  two values, `UserInterface` and `Monospace`.
- A font packed inside the extension's MSIX is private to the extension's process. The host draws in its own
  process and never sees it. An MSIX can publish a font to every app through `windows.sharedFonts`, and the host
  would use it only under a family name it already asks for.
- `Monospace` makes the host ask for `"Cascadia Mono, Consolas"`. Windows Terminal publishes Cascadia Mono to every
  app through `windows.sharedFonts`. Its Cascadia Mono 2407.024 on the owner's laptop has all seven eighth blocks
  `▏▎▍▌▋▊▉` plus `█` and `░`, all at one advance width.
- `PlainTextContent` is not in SDK 0.9.260303001, which this repository pinned; it is in 0.12.260812002. A
  `PlainTextContent` updates its text in place, without the rebuild a markdown block gets.

## Decision
1. **Eighth-cell steps.** `SpeedFormatter.Bar` fills the same 24 cells in eighths: full cells `█`, one partial cell
   `▏` to `▉` for the remainder, `░` for the rest. 192 steps instead of 48. It still rounds down, is full only at
   the scale, and keeps a constant length.
2. **The bar in a monospace plain-text block.** Each meter becomes two blocks: a `MarkdownContent` with the title,
   the active marker and the H2 readout (`MeterMarkdown.Readout`), and under it a `PlainTextContent` with
   `FontFamily.Monospace` and no word wrap holding the bar and its scale line (`SpeedFormatter.BarWithScale`). The
   page returns six fixed blocks instead of four. The fenced code block is gone from the meter.
3. **SDK 0.12.** `Microsoft.CommandPalette.Extensions` goes from 0.9.260303001 to 0.12.260812002, the version
   PowerToys 0.101 ships with. Same package, same publisher, same single dependency (`Microsoft.Windows.CsWinRT`
   2.2.0, already pinned); no new package.
4. **No bundled font now.** The extension relies on the Cascadia Mono that Windows Terminal publishes. Bundling
   Cascadia Mono (SIL Open Font License 1.1, so redistribution is allowed) through `windows.sharedFonts` is a
   separate decision, taken only if the VM run shows Windows Terminal can be missing where the extension runs. It
   would install a font for every app on the PC, a new capability under `docs/SAFETY-CONTRACT.md` §3 question 4.

Not chosen:
- **Fira Code's progress glyphs or Sparks**: the host never draws in a font the extension picks.
- **Eighth blocks in fenced code**: Consolas lacks six of them; the fallback font is narrower and breaks the grid
  (ADR-0017).
- **Publishing our own copy named "Cascadia Mono" now**: it would duplicate Windows Terminal's copy for every app
  on the PC, and which copy wins is unknown.

## Consequences
- The bar is four times finer. With the 100 ms easing of ADR-0017 it moves in steps of 1/192 of the scale.
- The meter needs PowerToys 0.99 or later (`PlainTextContent`). On an older host the bar block does not render and
  the readout still does (Inferred: the host has no template for an unknown content type). The owner's laptop and
  the VM run 0.101.
- Where Windows Terminal is missing, the host falls back to Consolas and the partial cells come from another font:
  the bar stays readable but its last cell is narrower. Recorded as Unknown 8 in `docs/CMDPAL-RENDERING.md` §16
  until the VM run.
- The bar block has 12/8/8/8 DIP padding and no card border, unlike the fenced block; the user can switch it to
  the UI font or zoom it from its context menu. How it lines up under the readout is checked on the VM.
- A plain-text block is selectable text; selecting the bar is harmless.
- `docs/SAFETY-CONTRACT.md` §3: questions 1, 2, 3 and 5 stay "No". Question 4: no new dependency or capability;
  an existing dependency moves to a newer version, recorded here, and `Directory.Packages.props` (safety-sensitive)
  changes for that reason only.
