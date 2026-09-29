# How Command Palette renders extension content

Research for plan item 5.1 (the gauge), session 5, 2026-09-29. Written for the building agent that redraws the
meter view; the owner's version is `docs/CMDPAL-RENDERING.html` (same facts, fewer citations). Nothing here was
run on Windows: every statement comes from reading source and documentation, and is labelled.

- **Verified**: read in the PowerToys or toolkit source, or in a Microsoft document; the citation follows.
- **Inferred**: follows from verified code, but the behaviour was not observed running.
- **Unknown**: could not be established; listed in §16 as something to check on the owner's PC.

## 0. Sources and how to read the citations

| Source | Revision read | Why this revision |
|---|---|---|
| `microsoft/PowerToys`, `src/modules/cmdpal/` | tag `v0.101.2362.0` (2026-08-25, the 0.101 release), tag `v0.101.2684.0` (2026-09-26, the latest 0.101 build), and `main` at `353caee0` (2026-09-29) | The owner's VM runs PowerToys 0.101.2652.0 (session 3 and 4 hand-offs), which lies between the two tags. Every file cited below was diffed between the release tag, the latest tag and `main`; where the text says "identical", the `main` line numbers apply to the owner's build as well. Post-0.101 changes are marked as such. |
| `CommunityToolkit/Labs-Windows`, `components/MarkdownTextBlock` | `main` at `89328136` (last change 2026-01-15) | PowerToys 0.97 to 0.101 pin `CommunityToolkit.Labs.WinUI.Controls.MarkdownTextBlock` 0.1.260116-build.2514 (`Directory.Packages.props:32`), built the day after that last change. |
| `microsoft/AdaptiveCards`, `source/uwp` and `source/shared` | `main` at `8b62e1d5` (2026-08-27) | PowerToys pins `AdaptiveCards.Rendering.WinUI3` 2.2.4-beta (2026-01-07), which has no git tag; `main` is the nearest source. Minor drift is possible. |
| Microsoft Learn, Command Palette extension pages | fetched 2026-09-29 | `learn.microsoft.com/windows/powertoys/command-palette/…` |
| PowerToys release notes, issues and pull requests | fetched 2026-09-29 | Cited by number. |

Paths without a prefix are relative to `src/modules/cmdpal/` in PowerToys. `Labs/…` is relative to
`components/MarkdownTextBlock/src/` in the toolkit repository. `AC/…` is relative to `source/` in AdaptiveCards.
Line numbers are from the `main` revisions above unless the text names a tag.

The raw reports of the five research subagents (one per surface, with more line-level detail than fits here)
are not in the repository; this document is their merge.

## 1. The short answer

Command Palette gives an extension five kinds of content on a `ContentPage` (markdown, an Adaptive Card, plain
text, one image, a tree of the former) plus list pages with icons, tags, a details pane and three grid layouts.
Nothing moves a value on the extension's behalf (no easing, no transitions, no SVG animation; an animated GIF
plays its own frames, §4, and cannot show a value): every visual change is the extension replacing a value, the host
re-reading it over COM, batching for 40 ms, and rebuilding the affected control on its UI thread. Markdown is
rebuilt whole on every `Body` change, images inside it are recreated empty and filled asynchronously, and a
`data:` image cannot carry size hints and is capped at 256 device-independent pixels wide. That combination is
what the session 4 gauge ran into (§14). The realistic ceiling for a smooth-looking meter on 0.101 is a small
number of updates per second with the extension interpolating between measurements, in a content block that
holds nothing but the moving part (§15).

## 2. The surfaces an extension can draw on

| Surface | Interface | Since | What the host renders it with | Live update path |
|---|---|---|---|---|
| Markdown block | `IMarkdownContent { String Body }` | 0.90 | CommunityToolkit Labs `MarkdownTextBlock` (Markdig → native `RichTextBlock`) | `Body` set → whole block re-parsed and rebuilt (§5) |
| Adaptive Card | `IFormContent { TemplateJson; DataJson; StateJson; SubmitForm }` | 0.90 | `AdaptiveCards.Rendering.WinUI3` 2.2.4-beta | `DataJson`/`TemplateJson` set → whole card rebuilt (§9) |
| Plain text | `IPlainTextContent { Text; FontFamily; WrapWords }` | 0.99 | selectable `TextBlock` with monospace and wrap toggles | `Text` set → in place |
| Image | `IImageContent { IIconInfo Image; MaxWidth; MaxHeight }` | 0.99 | `Viewbox` → `IconBox` (the icon pipeline, §6) | `Image` set → icon reloaded in place |
| Tree | `ITreeContent { RootContent; GetChildren() }` | 0.90 | nested repeater | `RaiseItemsChanged` |
| List row | `IListItem { Title; Subtitle; Icon; Tags; Details; … }` | 0.90 | fixed 44-DIP row, 20-px icon, up to three tag pills | property set → in place, no `RaiseItemsChanged` needed (§7) |
| Grid tile | `IListPage.GridProperties` = Small / Medium / Gallery | 0.95 | 32-px icon / 100×100 cell / 160×160 tile | as list rows |
| Details pane | `IDetails { HeroImage; Title; Body (markdown); Metadata }` | 0.90; live since 0.101 | card beside the list, 64-px hero, markdown body | property set → in place (0.101) |
| Status / progress | `IStatusMessage { State; Progress; Message }`, `IPage.IsLoading` | 0.90 | `IsLoading`: indeterminate bar. `Progress`: **not rendered** (§10) | property set |

Verified: `extensionsdk/Microsoft.CommandPalette.Extensions/Microsoft.CommandPalette.Extensions.idl` lines
339–366 (`IContent`, `IFormContent`, `IMarkdownContent`, `ITreeContent`, `IContentPage`), 474–492
(`IImageContent`, `IPlainTextContent`); `Microsoft.CmdPal.UI/Converters/ContentTemplateSelector.cs:25–37`.
The IDL diff between tags shows `IImageContent` and `IPlainTextContent` arriving in 0.99 (PR #43964) and no
graph, chart, XAML or HTML content type in any tag or on `main`.

Content blocks stack **vertically only**: `ContentPage.xaml:40–43, 141–148` is a `ScrollView` around an
`ItemsRepeater` with a vertical `StackLayout Spacing="8"`; each block sits in a `Grid Margin="0,4,4,4"
Padding="12,8,8,8"` (`ContentPage.xaml:61–90`). Two blocks can never be side by side. Inside one markdown
block, two images in one paragraph or a pipe table with an image per cell flow horizontally (§3).

## 3. Markdown: engine, constructs, styling

**Engine (Verified).** `Microsoft.CmdPal.UI.csproj:124` references `CommunityToolkit.Labs.WinUI.Controls.MarkdownTextBlock`;
the control parses with Markdig 0.38 and HtmlAgilityPack 1.11.71 (`Labs/CommunityToolkit.WinUI.Controls.MarkdownTextBlock.csproj:24–26`)
and renders the syntax tree into one native `RichTextBlock` (`Labs/Renderers/WinUIRenderer.cs`,
`Labs/TextElements/MyFlowDocument.cs:20`). It is not WebView2 and not the old `CommunityToolkit.WinUI.UI.Controls.Markdown`.

**Pipeline flags (Verified).** All extension flags default to off (`Labs/MarkdownTextBlock.Properties.cs:33–100`);
the content page enables exactly two, `UseEmphasisExtras` and `UsePipeTables` (`ContentPage.xaml:69–73`; the
details pane the same, `Pages/ShellPage.xaml:209–213`; the nested template inside `TreeContent` enables neither,
`ContentPage.xaml:98–102`).

| Construct | 0.101 | Notes |
|---|---|---|
| Headings, emphasis, lists, code, quotes, rules, links, images | on | CommonMark core |
| Pipe tables | on | cells are full `RichTextBlock`s, so images work inside cells; column alignment from `:---:` honoured (`Labs/TextElements/MyTableCell.cs:57–67`) |
| `~~strike~~`, `~sub~`, `^sup^` | on | `Labs/Renderers/ObjectRenderers/Inlines/EmphasisInlineRenderer.cs:19–34` |
| Raw HTML | on (`DisableHtml` false) | parsed, partly honoured, never shown as text; see below |
| Task lists, autolinks, list extras (`a.`, `i.`), soft-break-as-hard-break | off | not enabled |
| Emoji shortcodes (`:smile:`), math, footnotes, attributes, custom containers | not available | never in the pipeline |
| Text selection | off | `IsTextSelectionEnabled` default false, not set by the host |

**Styling (Verified, `Labs/MarkdownThemes.cs` defaults with host overrides in `ContentPage.xaml:25–33`).**
H1 22 px, H2 20 px, **H3 12 px and normal weight** (host override; smaller than body text, reads as a caption),
H4 16, H5 14, H6 12; headings semi-bold with 16-DIP top margin. Bold is semi-bold. Inline code is a bordered
`TextBlock` at **10 px**, not monospace. Fenced code blocks are Consolas in a card-coloured border, no syntax
highlighting (`Labs/TextElements/MyCodeBlock.cs:20–76`). A soft line break becomes a space; two trailing
spaces, a backslash, or `<br>` become a line break (`Labs/TextElements/MyParagraph.cs:87–115`,
`Labs/HtmlWriter.cs:27–30`). Bullets are text ("• ", "1. ") with 32-DIP indents; nesting works.

**Raw HTML (Verified, `Labs/HtmlWriter.cs:14–106`, `Labs/TextElements/Html/MyBlock.cs:24–34`, `MyInline.cs:21–31`).**
Block tags (`div p h1–h6 pre table details …`) become paragraphs; `align="left|right|center|justify"` on them
and on headings is honoured, which is the only way to centre an image. `<br>`, `<a>` and `<img src width height>`
are honoured. Every other inline tag (`b i span font code u sup`) loses its formatting and attributes;
`style=` and `color=` have no effect anywhere. Inside HTML blocks all tabs and newlines are stripped before
parsing (`Labs/Renderers/ObjectRenderers/HtmlBlockRenderer.cs:31–32`), so `<pre>` loses its lines. Inline
`<svg>`, `<style>`, `<script>` text renders as plain text (Inferred).

**Unicode (Inferred).** Block elements (`█▓▒░▁▂▃▄▅▆▇`, braille) are plain `Run` text in the UI font, or
Consolas inside a code block; font fallback on Windows 11 covers them, but equal glyph widths in the inline-code
font are not verified.

**Theme (Verified).** Text, headings, links, code and table brushes come from the host theme
(`Labs/MarkdownThemes.cs:37–128`, `ContentPage.xaml:25–33`). Markdown cannot set a colour, and nothing tells
the extension which theme or accent is active: the IDL has `IPage.AccentColor` and `ITag.Foreground/Background`
going extension → host and `IIconInfo.Light/Dark` for icons, and no property in the other direction
(`…Extensions.idl:27–31, 161–166, 255–262`). The window is translucent or acrylic since 0.97 (settings page:
backdrop style, opacity, background image), so an opaque image background looks boxed.

## 4. Images inside markdown

**Schemes (Verified, Learn "Display markdown content in Command Palette extensions", updated 2026-04-21;
`Microsoft.CmdPal.UI/Helpers/MarkdownImageProviders/CompositeImageSourceProvider.cs:9–14`).** Since 0.95:
`https:` (host downloads, 30 s timeout), `file:` (absolute paths only), `data:` (base64 or percent-encoded),
`ms-appx:` and `ms-appdata:` (resolve against the host package, useless for an extension). A URL that is not
well-formed is silently replaced by `#` and never loads (`Labs/Renderers/ObjectRenderers/Inlines/LinkInlineRenderer.cs:17–27`),
so a raw `data:image/svg+xml;utf8,<svg …>` with spaces and quotes does not work in markdown; base64 or
percent-encoding does. The Learn page keeps its pre-0.95 warning: the control could hang on images over
5 MB or 4000×4000 px or markdown over 1 MB; keep images small. The sample adds "Parsing large data blocks
the UI" (`ext/SamplePagesExtension/Pages/SampleMarkdownImagesPage.cs:59–60`).

**Size hints (Verified, `Helpers/MarkdownImageProviders/ImageHints.cs:23–47`; documented on the Learn page and
in the sample, lines 152–167).** Query parameters on `https:`, `file:`, `ms-appx:`, `ms-appdata:` URLs:

| Hint | Effect |
|---|---|
| `--x-cmdpal-width=<dip>` / `--x-cmdpal-height=<dip>` | forced width / height; height also sets the SVG rasterisation height |
| `--x-cmdpal-maxwidth` / `--x-cmdpal-maxheight` | caps |
| `--x-cmdpal-fit=fit` | scale to the column width (default `none`) |
| `--x-cmdpal-upscale=true` | allow enlarging (default downscale only) |

**`data:` URIs get no hints.** `DataImageSourceProvider.cs:69–72` returns only `DownscaleOnly = true` and never
parses the query; the sample says it in words: "Currently no support for data: scheme as it doesn't support
query parameters at all" (`SampleMarkdownImagesPage.cs:167`).

**SVG detection and decoding (Verified, `Helpers/MarkdownImageProviders/ImageSourceFactory.cs:42–144`).** An
image is SVG if the content type says so, the URL contains `.svg`, or the first 1 KB contains `<svg`. The
host then loads the whole document with `XDocument` to read the root `width` and `height` attributes as plain
numbers (`"256px"`, `"100%"`, `"16em"` parse as 0), creates a `SvgImageSource`, and sets its rasterisation size
from them. All of this runs on the UI thread. Bitmaps go to `BitmapImage` (WIC: PNG, JPEG, BMP, GIF, TIFF,
JPEG XR, ICO).

**Sizing rules (Verified code path, `Helpers/MarkdownImageProviders/RtbInlineImageFactory.cs:110–249`; visual
result Inferred).** Without hints the image gets `Stretch = None`, `Width` = the SVG's `width` attribute,
`MaxWidth = min(width, 256)` for SVG, and a rasterisation width scaled by the monitor's DPI while the
rasterisation **height is not scaled**. So:

1. A `data:` SVG can never be wider than 256 DIP, and cannot ask for `fit`.
2. Without numeric `width`/`height` on the root `<svg>` the natural width is `int.MaxValue` and the
   rasterisation size overflows; always give the root numeric `width`, `height` and a `viewBox`.
3. At 125 % or 150 % scaling the width is rasterised at scale and the height is not, which plausibly
   explains a gauge that looks squashed, soft, or offset (Inferred; the owner's "layout not very good").
4. `<img width height src="data:…">`, the sample's recommended way to size a data image, sets `MaxWidth`
   and `MaxHeight` in Labs build 2514 (`Labs/TextElements/MyImage.cs:184–193`) and `MaxWidth` is then
   overwritten by the factory (`RtbInlineImageFactory.cs:179`); only `MaxHeight` survives, and `Stretch`
   stays `None`. In the 0.95/0.96 build the attributes set `Width`/`Height` directly. Treat the HTML width
   attribute as unreliable on 0.97 and later (Inferred from both versions' source).

**What the SVG renderer understands (Verified, Learn "Direct2D SVG Support"; `SvgImageSource` remarks).**
Elements: `svg g defs use path rect circle ellipse line polyline polygon linearGradient radialGradient stop
clipPath image title desc`. Presentation attributes: `fill fill-opacity fill-rule stroke stroke-width
stroke-opacity stroke-linecap stroke-linejoin stroke-dasharray stroke-dashoffset stroke-miterlimit opacity
clip-path transform visibility display`, and the per-element `style="…"` attribute with those properties
(Microsoft's own Performance Monitor charts use it, `ext/Microsoft.CmdPal.Ext.PerformanceMonitor/DevHome/Helpers/ChartHelper.cs:26–49`).
**Not understood, silently dropped**: `<text>`, `<tspan>`, `<style>` blocks and CSS classes, `<filter>`,
`<mask>`, `<pattern>`, `<marker>`, `<symbol>`, `<foreignObject>`, every `<animate*>` element ("SvgImageSource
supports secure static mode … and does not support animations or interactions"), `em`/`ex` units, and remote
`<image>` references. Eight-digit `#RRGGBBAA` colours are not in SVG 1.1; use `stroke-opacity` (session 4
found this the hard way). Numbers on a gauge must be markdown text next to the image, or outlined paths.

**Animated formats.** `BitmapImage` plays animated GIF with `AutoPlay` on by default (Learn, `BitmapImage`
"Animated images"); nothing in the host turns it off, so a GIF should animate in markdown (Inferred, not
run), and it restarts on every `Body` change because the image element is recreated. APNG is not a WIC
format (first frame at best, Inferred). WebP depends on an installed codec (Unknown).

**Caching: none.** Neither `ImageProvider.cs` nor the three source providers keep any cache; every render
creates a new image, re-decodes the data URI, parses the SVG twice (size probe and Direct2D) and creates a new
`SvgImageSource` (Verified by absence).

**Alignment.** An image is an inline element on the text baseline, left-aligned. `<p align="center">` or
`<div align="center">` around it centres it (§3).

## 5. The update pipeline: from `Body = …` to pixels

Verified end to end (PowerToys tag `v0.101.2684.0`, identical to the 0.101 release and so to the owner's 0.101.2652 for every file named;
Labs build 2514).

1. **Extension.** `MarkdownContent.Body { set => SetProperty(ref field, value); }`
   (`extensionsdk/Microsoft.CommandPalette.Extensions.Toolkit/MarkdownContent.cs:9`). `SetProperty` does
   nothing when the string is equal (`BaseObservable.cs:38–48`) and otherwise raises the WinRT `PropChanged`
   event **synchronously, in line** (`EventHelpers.cs:14–53`; the toolkit's own comment: "We probably want to
   have OnPropertyChanged raise the event asynchronously, so as to not block the extension app",
   `BaseObservable.cs:10–13`). A handler that fails with `RPC_E_DISCONNECTED` or `RPC_S_SERVER_UNAVAILABLE`
   is unsubscribed (PR #50485, in 0.101.2652).
2. **Host, COM callback thread.** `ContentMarkdownViewModel.Model_PropChanged` → `FetchProperty("Body")`
   reads `model.Body` once more over COM (one nested cross-process call; the `IContent[]` array is **not**
   re-fetched) and calls `UpdateProperty` (`Microsoft.CmdPal.UI.ViewModels/ContentMarkdownViewModel.cs:34–63`).
   The extension's setter therefore blocks for two cross-process hops; both processes are MTA, so this is
   safe, and the host does nothing heavy inside the callback (`ExtensionObjectViewModel.cs:19–21`).
3. **Batching.** `MarkPropertyDirty` queues the name without de-duplication ("We should re-consider if this
   worth deduping", `ExtensionObjectViewModel.cs:128–137`) and `BatchUpdateManager` arms a one-shot **40 ms**
   timer (the comment says 30, the constant is 40, `BatchUpdateManager.cs:15–18`); the flush runs on the
   thread pool and posts one task to the page's UI scheduler that raises `PropertyChanged` once per queued
   name (`ExtensionObjectViewModel.cs:139–215`). Introduced by PR #44545 in 0.98 to stop "tearing" of list
   rows. There is no other throttle on the content-page path.
4. **UI thread.** `Text="{x:Bind Body, Mode=OneWay}"` (`ContentPage.xaml:71`) sets `MarkdownTextBlock.Text`
   → `ApplyText(true)`: `RichTextBlock.Blocks.Clear()`, full Markdig parse, full rebuild of every paragraph,
   run, table and image (`Labs/MarkdownTextBlock.xaml.cs:45–51, 90–106`, `Labs/Renderers/WinUIRenderer.cs:46–51`).
   No diffing, no incremental update.
5. **Images after the rebuild.** Every `![…](…)` becomes a new **empty** `Image` in an `InlineUIContainer`;
   loading starts on its `Loaded` event, after layout, and the host provider then **replaces** the element
   after an `await` (`Labs/TextElements/MyImage.cs:84–102`). Between the clear and the swap the image has
   no size, so the block collapses and re-expands. At 5 Hz that is the flash the owner saw (structure
   Verified; visibility of the flash Inferred).
6. **Control identity.** The view model instance is unchanged, so the `ItemsRepeater` keeps the same
   `MarkdownTextBlock` and the `ScrollView` is not reset; scroll offset survives unless the content shrinks.
7. **`RaiseItemsChanged()` is worse.** `ContentPageViewModel.Model_ItemsChanged` runs `FetchContent()`
   synchronously on the callback thread ("TODO: Does this need to hop to a different thread…",
   `ContentPageViewModel.cs:56–57`), calls `GetContent()` back into the extension, builds **new** view
   models for every block (`ContentViewModel` has no `Equals`), and `InPlaceUpdateList` replaces them all,
   destroying and recreating every control and re-running the shell's focus retry loop ("focusing page
   with MarkdownTextBlock takes up to 5 attempts", `Pages/ShellPage.xaml.cs:903–905`). An extension that
   raises it from inside `GetContent()` recurses; this is the freeze session 2 hit. The content page has
   none of the generation guards `ListViewModel` has.

**Cadence (Inferred from the above; no benchmark exists anywhere).** Two sets within 40 ms are merged into
one UI pass; the pipeline cannot show more than about 25 distinct frames per second; each frame is a full
rebuild; nobody in the PowerToys repository drives a `MarkdownContent` faster than 1 Hz (the samples update
list titles at 500 ms and a details body at 1 s; the Performance Monitor updates its card at 1 s). For a
few-hundred-byte block with one small SVG the CPU cost is plausibly milliseconds, so 5 to 10 Hz is not
CPU-bound; what limits perceived quality is the discrete steps between measurements and the empty-image
frame per rebuild. Text-only blocks skip step 5 entirely.

## 6. Icons: `IconInfo` and `IconData`

Every icon in the host (list rows, tags, grid tiles, the details hero image, `ImageContent`, toasts) goes
through one pipeline: `IconBox` → `IconProvider` (one provider per size bucket, some cached) →
`IconLoaderService` (up to 4 workers) → for strings, Terminal's `IconPathConverter` in C++; for streams,
`BitmapImage.SetSourceAsync`. All Verified and byte-identical between 0.101 and `main`
(`Microsoft.CmdPal.UI/Helpers/Icons/*`, `Microsoft.Terminal.UI/IconPathConverter.cpp`).

| Icon string or data | 0.101 result | Evidence |
|---|---|---|
| Segoe Fluent glyph (`""`) | `FontIcon`, Fluent family, sized to the slot | `IconPathConverter.cpp:202–204` |
| One emoji (ZWJ sequences allowed), one ASCII letter or digit | `FontIcon` in Segoe UI Emoji / Segoe UI | `:206–210`; `ext/SamplePagesExtension/Pages/SampleIconPage.cs` |
| Two or more characters (`"42"`, `"WM"`, two emoji) | invalid: dotted circle U+25CC | `:220`; `SampleIconPage.cs` |
| Absolute path or `https:` to `.png .jpg .ico .bmp .gif` | `BitmapImage` decoded at slot × scale | `:130–141` |
| Absolute path or `https:` to `.svg` | `SvgImageSource`, rasterised at slot × scale | `:114–127` |
| `file.exe,3`, `.dll,index`, `.lnk` | shell icon via `SHDefExtractIcon` | `:315–367` |
| `%systemroot%\…` | **not expanded** by the host (the call is commented out) | `:155` |
| `data:…` string | goes to `BitmapImage.UriSource`, which documents no `data:` scheme → blank (Inferred); still unsupported on `main` (`Helpers/Icons/IconPathConverter.cs:63` checks for a `.svg` path) | |
| `IconData(IRandomAccessStreamReference)` / `IconInfo.FromStream` | `OpenReadAsync` over COM, then `BitmapImage.SetSourceAsync`; **raster only**, an SVG stream fails | `IconLoaderService.cs:169–196` |
| `IconInfo(light, dark)` | the host picks per theme and re-requests on theme change | `Helpers/Icons/IconProvider.cs:56–62`, `Controls/IconBox.cs:125–171` |

A non-empty `Icon` string wins over `Data` (`IconLoaderService.cs:162–167`).

**Size buckets and caches (Verified, `Helpers/Icons/IconServiceRegistration.cs:19–41`, `WellKnownIconSize.cs`).**
16 px uncached (toasts, some chrome); 20 px cached, 1024 entries (list rows, tag icons at 12 DIP, context
menus); 32 px uncached (small grid tiles); 64 px cached, 256 entries (medium tiles at 48 DIP, the details hero
at 64 DIP wide, empty-content icon); 256 px cached, 64 entries (gallery tiles 160×160); unbounded, uncached,
priority queue (`ImageContent`). Decode size is bucket × display scale. The cache key is the icon **string**,
font family, the stream reference's **object identity**, and the scale (`CachedIconSourceProvider.cs:93–119`),
with a 60-minute decay and eviction above capacity. Consequences: a path string that names a pre-rendered
frame is decoded once and then served from the cache at every bucket; a new stream object per frame always
decodes; reusing one stream object after changing its bytes returns the stale bitmap at cached sizes.

**Swap mechanics (Verified, `Controls/IconBox.cs:178–228`).** A new `FontIcon` or `BitmapIconSource` is
swapped into the existing element; PNG, SVG and stream results (`ImageIconSource`) create a new element. No
cross-fade; list and grid item transitions are empty collections (`ExtViews/ListItemsView.xaml:628–630, 672–674`).
Post-0.101, PR #50189 makes `IconBox` alternate between two slots instead of rebuilding.

**Icons and markdown images do not share code.** Both end in `SvgImageSource` (Direct2D), but the markdown
path (§4) has its own providers, no cache and no size buckets.

**Post-0.101 (on `main`, not in 0.101; Verified from the PRs).** PR #50190 (merged 2026-09-29) adds generated
icons `|Swatch|#0067C0|` and `|Initials|JP|info|square|` (one to three graphemes); PR #50191 (open) adds
`|Svg|<path or inline svg>` and `|ThemedSvg|accent|…` with `{{ThemeColor}}` / `{{AccentColor}}` substitution;
PR #50188 adds `|AppIcon|`. PR #50187 splits glyph and image caches. Which of these reach 0.102 is Unknown.

## 7. List pages: rows, tags, details pane, grids

**Row (Verified, `ExtViews/ListItemsView.xaml:36–42, 347–423`).** Fixed 44 DIP tall for virtualisation;
columns 28 | * | auto; 20-DIP icon drawn in the secondary text colour; Title and Subtitle 14 px, one line each,
ellipsis; tags right-aligned, at most three pills plus a `+N` pill with the rest in its tooltip
(`ListItemViewModel.cs:15, 301–329`; 0.100 release note).

**Tags (Verified, IDL 146–167; `Controls/Tag.xaml`, `Tag.xaml.cs:80–152`).** `Text`, `Icon` (12 DIP),
`Foreground`, `Background` as `OptionalColor` with **arbitrary ARGB honoured** (foreground only: text and
border; background only: background and border; both: as given), 12-px font, no automatic contrast. The host
copies tag values once and re-reads them only on `PropChanged("Tags")` of the item.

**Live updates without `RaiseItemsChanged` (Verified, `ListItemViewModel.cs:148–181`,
`CommandItemViewModel.cs:339–443`; 0.96 note "labels and icons of list items … update when they change").**
`Title`, `Subtitle`, `Icon`, `Tags`, `TextToSuggest`, `Section`, `Details`, `Command`, `MoreCommands` on an
existing item update in place. `RaiseItemsChanged` is for a changed *set* of items: the host calls
`GetItems()` on a background thread, reuses view models for the same `IListItem` instances, and merges with
`InPlaceUpdateList` (no `Clear()`, no flash, `ListViewModel.cs:347–500`); passing `IncrementalRefresh` keeps
the selection, otherwise it may jump to the first item. The Pomodoro extension avoids timer-driven list
refreshes for exactly that reason.

**Details pane (Verified, `Pages/ShellPage.xaml:443–547, 618–627`; `Converters/DetailsSizeToGridLengthConverter.cs:24–31`).**
The list column is `3*`, `2*` or `1*` for `ContentSize.Small/Medium/Large` (0.97) against a `2*` pane, so the
pane takes about 40 %, 50 % or 67 % of the width. Inside: hero `IconBox` 64 DIP wide (theme-aware, height
follows the aspect), Title 18 px semi-bold, **Body as a `MarkdownTextBlock` with the same image provider as
content pages** (so `data:` images work there), Metadata as links, commands, separators and tag wrap panels.
Since 0.101 (PR #48070) `Title`, `Body`, `HeroImage` and `Metadata` update live from `PropChanged` on the
`Details` object (`Microsoft.CmdPal.UI.ViewModels/DetailsViewModel.cs`; `SampleLiveDetailsPage.cs:72–92`
updates at 1 s). Post-0.101 `IDetails2.GetContent()` puts `IContent[]` in the pane.

**Grids (Verified, IDL 297–316; `ListItemsView.xaml:30–34, 458–591`; 0.95 note).** Exactly three layouts:
`SmallGridLayout` (32-DIP icon only, tooltip = title, uncached bucket), `MediumGridLayout { ShowTitle }`
(100×100 cell, 48-DIP icon, 12-px title), `GalleryGridLayout { ShowTitle; ShowSubtitle }` (160-wide tile,
160×160 `Viewbox UniformToFill` around the icon, 14-px title, 11-px subtitle, text lines reserved). Tags are
not shown in tiles. The docs' `TileSize` property no longer exists (removed from the IDL in 0.95). Updates
go through the same `RaiseItemsChanged`/property path as rows, with a grouped projection diffed the same way.

## 8. `ImageContent` and `PlainTextContent` (0.99 and later)

**`ImageContent` (Verified, `ExtViews/Controls/ImageContentViewer.xaml(.cs)`, `ContentImageViewModel.cs`).**
A bordered card with a centred `Viewbox Stretch="Uniform"` whose `MaxWidth`/`MaxHeight` come from the content
(`-1` = unlimited; height additionally clamped to the page height minus 52 DIP), an `IconBox` on the unbounded
uncached priority bucket, an always-present "Zoom and scroll" overlay button and a context menu (open viewer,
copy image, copy link). Setting `Image` raises `PropChanged` and the viewer swaps only the image (no markdown
parse, no block rebuild). Sources are the icon rules of §6: a `.svg` **file path** is rasterised (at 256 px
wide for the unbounded request, then scaled by the `Viewbox`, so large SVGs look soft); a stream is raster
only; a `data:` string does not work. The old image stays until the new one is ready (Inferred from the async
request in `IconBox.cs:209–222`), which is plausibly less blinking than markdown; Unverified.

**`PlainTextContent` (Verified, `ExtViews/Controls/PlainTextContentViewer.xaml`).** A selectable `TextBlock`
with monospace and word-wrap toggles and zoom; `Text` updates in place. Inside a `TreeContent` neither
`ImageContent` nor `PlainTextContent` has a template (`ContentPage.xaml:115–132`; Inferred: renders nothing).

## 9. Adaptive Cards (`FormContent`)

**Renderer (Verified, `Directory.Packages.props:10–16`).** `AdaptiveCards.Rendering.WinUI3` 2.2.4-beta,
`AdaptiveCards.ObjectModel.WinUI3` 2.0.2-beta, `AdaptiveCards.Templating` 2.0.6,
`Microsoft.Bot.AdaptiveExpressions.Core` 4.23.1. The version gate is **1.6**
(`AC/uwp/SharedObjectModel/lib/AdaptiveCard.cpp:39`); a higher `version` yields a "couldn't be displayed"
fallback card. The bundled samples declare 1.6, the Performance Monitor 1.5.

**Elements that render (Verified, `AC/shared/cpp/ObjectModel/Enums.h:223–252`, `AC/uwp/SharedRenderer/lib/Util.cpp:643–675`).**
`TextBlock`, `RichTextBlock`, `Image`, `ImageSet`, `Container`, `ColumnSet`/`Column`, `FactSet`, `Table`,
`Carousel`, `Media`, `ActionSet`, all `Input.*`; actions `OpenUrl`, `Submit`, `Execute`, `ShowCard`,
`ToggleVisibility`. **Not implemented, silently dropped** (unknown elements fall back to nothing,
`AC/uwp/SharedRenderer/lib/XamlBuilder.cpp:288–335`): `ProgressBar`, `ProgressRing`, `Icon`, `Badge`, every
`Chart.*` (including `Chart.Gauge`), `CodeBlock`, `CompoundButton`, `Layout.*`, `Input.Rating`. The online
designer offers these under "1.6 preview"; a card that uses them will render without them (issue #46419
reports exactly this).

**Host config (Verified, `Microsoft.CmdPal.UI/Controls/AdaptiveCardsConfig.cs`).** Chosen once per control from
the theme at construction (issue #49435 open; PR #50151 that tried to re-theme was closed unmerged on
2026-09-22). Fonts: Segoe UI 12/14/14/20/26 px for small/default/medium/large/extraLarge, weights 200/400/600;
monospace is Courier New 12/12/14/18/26. Spacing 4/8/20/30/40. Separator line thickness 0. Container styles:
`default` transparent, `emphasis` about 3.5 % white in dark mode; `good`, `attention`, `warning`, `accent`
are not defined and fall back to the renderer's light pastel backgrounds with black text (poor in dark mode;
outcome Inferred). Named text colours only (`default dark light accent good warning attention`, plus
`isSubtle`); no hex colours on text. Image size presets are 16/24/32 px, so use explicit `"width": "NNpx"`.
`allowCustomStyle` is false (card-level style ignored). Every `TextBlock` is selectable.

**`Image` (Verified, `AC/uwp/SharedRenderer/lib/XamlBuilder.cpp:283–345`, `Util.cpp:678–690, 743–755`,
`AdaptiveImageRenderer.cpp:179–201, 375–424`).** `data:` with `base64` decodes any WIC format; `data:image/svg+xml;utf8,<svg …>`
works **raw, without base64** (the renderer extracts from `<svg`); this is how the Performance Monitor
draws its charts (`ChartHelper.cs:71–75`). SVG is rasterised from the document's `width`/`height`, and explicit
`"width"`/`"height"` in px become `MaxWidth`/`MaxHeight`. `https:` images are fetched by the host. Animated
GIF should play (`BitmapImage`, `AutoPlay` untouched; Inferred). SVG animation: no (static rasterisation).

**Layout (Verified).** `Column.width`: `stretch`, `auto`, a numeric weight, or `NNpx` (`XamlHelpers.cpp:210–219`);
`Container`: `style`, `bleed`, `minHeight`, `verticalContentAlignment`, `backgroundImage` (SVG allowed);
`Table` with weighted or px columns. `Action.ToggleVisibility` and `Action.ShowCard` are handled inside the
renderer (instant `Visibility` flips, no event to the host, `RenderedAdaptiveCard.cpp:64–94, 125–126`);
`Submit`/`Execute` reach `FormContent.SubmitForm(inputs, data)` on a host thread-pool thread
(`ContentFormViewModel.cs:145–176`); Enter in a single-line text box submits (0.101). Builds from
0.101.2572 add `IFormContent2.SubmitAction(actionId, …)` and CmdPal-specific input elements.

**Templating (Verified).** The **host** expands `TemplateJson` with `DataJson`
(`ContentFormViewModel.TryBuildCard`); `${…}`, `$data`, `$root`, `$index`, `$when` work; `$host` is null.
Functions from `ExpressionType.cs`: arithmetic, `min max round floor ceiling abs`, `formatNumber`, `if`,
`coalesce`, string and collection helpers, date/time; no `padLeft`. A malformed template renders an error card.

**Live update path (Verified, `ContentFormViewModel.cs:107–141`, `Controls/ContentFormControl.xaml.cs:81–150`).**
`DataJson` set → `PropChanged` → host re-reads it, re-expands the template, re-parses the JSON (on the COM
callback thread) → 40 ms batch (§5) → `ContentGrid.Children.Clear()` and a **complete XAML rebuild** of the
card. Images decode asynchronously again (a blank frame: issue #46362 "Graphs in performance monitor
flicker", open, milestone 0.102); input values, toggle state and selection are lost; and when the card is the
only content on the page, focus moves to its first focusable control after every rebuild
(`OnlyControlOnPage`, `ContentPageViewModel.cs:85–86`). `StateJson` changes after initialisation are ignored.

**What 0.102 changes (Verified from PR #50211, open, approved, auto-merge armed 2026-09-28, milestone 0.102).**
An `IncrementalAdaptiveCardUpdater` patches **only `TextBlock.text` and an inline-SVG `Image.url`** in place
when the card's shape is unchanged; anything else still rebuilds. A card made of text blocks plus one inline
SVG image is the shape that becomes flicker-free.

## 10. What animates natively, and what does not

Verified in `Pages/ShellPage.xaml` and the controls:

- `IPage.IsLoading` → an **indeterminate** `ProgressBar` under the search box with a 333 ms fade
  (`ShellPage.xaml:410–427`). This is the only progress visual an extension can switch on. The meter view
  already uses it.
- `IStatusMessage.Progress` (`ProgressState { IsIndeterminate; ProgressPercent }`) has a determinate
  `ProgressBar` in an `InfoBar` whose parent `StackPanel` is hard-coded `Visibility="Collapsed"` and never
  toggled (`ShellPage.xaml:580–604`, same at the 0.101 tag). Status messages are shown only as a badge on
  the command bar with a flyout of message text and severity (`Controls/CommandBar.xaml:101–130`), hidden on
  the root page. **A percentage in a status message is not rendered in 0.101.**
- Toasts (`CommandResult.ShowToast`, `ToastArgs { Icon; Command }` since 0.101) slide in a separate window;
  `ToastStatusMessage` is a 2.5 s status message, not a toast.
- List and grid items have empty `ItemContainerTransitions`; icons swap without cross-fade; the details pane
  fades and slides in over 187 ms; page navigation slides from the right unless the user disabled page
  transition animations. None of these are addressable by an extension.
- Adaptive Cards: no content animation beyond button press feedback.

## 11. The window

Verified: default 800×480 DIP, minimum 640×420, user-resizable by an 8-DIP grip, size and position
persisted (`Microsoft.CmdPal.UI/MainWindow.xaml:11–14`, `MainWindow.xaml.cs:111–115, 344–363`). A content
page cannot request a size; it scrolls. Per-monitor DPI; SVG rasterisation uses `XamlRoot.RasterizationScale`.
The markdown column width is whatever `RichTextBlock.ActualWidth` is at runtime (not a constant).

## 12. What Microsoft and other authors do

- **Performance Monitor (built in, Verified).** One `FormContent`; a `System.Timers.Timer(1000)` sets
  `DataJson` (`ext/Microsoft.CmdPal.Ext.PerformanceMonitor/DevHome/Helpers/DataManager.cs:19–28`,
  `PerformanceWidgetsPage.cs:428–478`); the template's `Image.url` is `${cpuGraphUrl}` with explicit px
  width and height (`DevHome/Templates/SystemCPUUsageTemplate.json:20–28`), and the value is
  `"data:image/svg+xml;utf8," + <svg>` of a fixed 268×86 area chart with 34 points (`ChartHelper.cs:23–24, 71–145`).
  It flickers (#46362), and the timer starts and stops with the page via the `ItemsChanged`
  subscribe/unsubscribe trick (`OnLoadStaticPage.cs`). The team is replacing the SVG with native graph
  content (issue #50430 "SVG, you're fired!", draft PR #50443 adding `ILineGraphContent`,
  `IDoughnutGraphContent`, `IVerticalUsageBarContent`, `IResourceBarContent`; no milestone, merge conflicts,
  awaiting an SDK sign-off since 2026-09-09).
- **Samples (Verified).** `SampleUpdatingItemsPage` sets three `ListItem.Title`s every 500 ms;
  `SampleLiveDetailsPage` sets `Details.Body` every 1 s; no sample updates a `MarkdownContent` on a timer.
- **Third-party (Verified from the repositories).** NetSpeed-CmdPal (LibreSpeed) is a plain list with
  "↓ x Mbps" rows and no progress. HardwareMonitorCmdPal draws its history as an ASCII chart in a fenced
  code block, refreshed at most every 2 s with a sticky Y axis, "drawn as code, not a bitmap".
  Pomodoro-CmdPal re-renders only on user input so the selection never jumps. No published extension
  animates anything inside a content page.
- **Raycast's speedtest (for comparison, Verified from `raycast/extensions`).** A `Detail` markdown view with
  a base64 SVG data URI gauge (300×240, 240° arc, a knob on the arc, a translucent progress arc), regenerated
  per sample with a frame key in the alt text for cache busting and a `?raycast-width=` hint; no SMIL or CSS
  animation. The same technique as the session 4 gauge, on a host whose markdown accepts a width hint on
  `data:` URIs.

## 13. Version history that matters here

| PowerToys | Rendering change (Verified from release notes and tags) |
|---|---|
| 0.95 (2025-10) | Grid layouts; `file:`/`data:`/`ms-appx:` images in markdown with the `--x-cmdpal-*` hints (PR #41754); pipe tables and emphasis extras enabled |
| 0.96 | List labels and icons update live |
| 0.97 (2026-01) | Labs `MarkdownTextBlock` bumped to build 2514 (image-scaling rewrite; the `<img width>` behaviour of §4 changed here); `ContentSize` for the details pane; grid keyboard navigation; translucent window options |
| 0.98 (2026-03) | 40 ms batching of property changes (PR #44545); Dock; Performance Monitor extension |
| 0.99 (2026-04) | `IImageContent` and `IPlainTextContent` (PR #43964); inline-code theme fix |
| 0.100 (2026-06) | SDK 0.11; parameter pages; tag pills capped at three |
| 0.101.2362 (2026-08-25) | Live details pane (PR #48070); toast icons and buttons; Adaptive Cards packages upgraded; SDK 0.12 (NuGet 0.12.260812002) |
| 0.101.26xx previews (Sept) | `IDetails2`, `IFormContent2`, failing handlers no longer block updates (#50483) |
| `main`, unreleased | generated icon protocols (#50190 merged, #50191 open); in-place Adaptive Card updates (#50211, 0.102); graph content (#50443, draft) |

The extension template pins `Microsoft.CommandPalette.Extensions` 0.11.260520004; this repository builds
against 0.9.260303001 (session 4). The Learn reference index (dated 2025-02) does not list `IImageContent`,
`IPlainTextContent` or the later interfaces.

## 14. What the session 4 gauge ran into

The session 4 build (`GaugeSvg`, commit `6034b22`; ADR-0011) embedded a 220×121 SVG as a base64 `data:` image
inside the one markdown block, regenerated on every progress report (200 ms). Against §4 and §5:

1. **Discrete steps.** The arc moved only when a measurement arrived; nothing in the host interpolates.
2. **A blank frame per update.** Every `Body` set rebuilt the whole block and recreated the image empty
   (§5 step 5); at 5 Hz the arc blinked and the text under it moved.
3. **No size control.** A `data:` image gets no hints, `Stretch = None`, a 256-DIP cap, a DPI-scaled
   rasterisation width and an unscaled height (§4). The result depends on the monitor's scaling.
4. **Small readouts.** The value sat under `###`, which the host renders at 12 px normal weight (§3).
5. **Everything in one block.** The heading, status, latency, both meters and the ISP line were re-parsed
   five times a second for one arc.

None of these is a bug in the gauge geometry, which the tests cover; they are properties of the host.

## 15. Options for plan item 5.1, ranked

Each option names the mechanism it relies on and what could go wrong. Costs are relative to the current
code. "Smooth" means no blank frame; "eased" means the value moves between measurements.

1. **Interpolate in the extension and render on a fixed tick.** Keep the 200 ms measurement cadence but
   drive the display from a timer (100 to 200 ms) that eases the shown value toward the last measurement,
   and set `Body` only when the rendered string changes (`SetProperty` already skips equal strings).
   Mechanism: §5 steps 1 to 3. Removes the jumping regardless of how the meter is drawn. Risk: none
   beyond one more timer; never tick below 40 ms (merged) and keep 100 ms as the floor for headroom.
2. **Split the page into blocks; churn only the moving one.** Return the same `MarkdownContent` instances
   from `GetContent()`: a static block (heading, status, latency, ISP line) and one block per meter, or one
   block for both meters. Only a block whose `Body` changed is rebuilt (§5 step 4, one `MarkdownTextBlock`
   per block). Never call `RaiseItemsChanged` on the meter path (§5 step 7). Risk: 8 DIP of extra spacing
   between blocks. Cost: small, in `MeterPage` and `MeterMarkdown`.
3. **A text-only meter in the moving block.** No image means no asynchronous stage and no blank frame (§5
   step 5). Raise the bar's resolution with fractional block glyphs (`▏▎▍▌▋▊▉█`) at a constant string length
   so nothing re-wraps; put the number in an H2 or bold text, not H3. HardwareMonitor ships this way.
   Risk: glyph widths in the inline-code font are Unverified; Consolas in a fenced block is the safe fallback.
   With options 1 and 2 this is the cheapest path to "smooth and eased", and it stays testable in Core.
4. **Keep an SVG gauge, but fix it and slow it.** Root `<svg>` with numeric `width`/`height` ≤ 256 and a
   `viewBox`; only §4's element subset; numbers as markdown text; transparent background; in its own block
   (option 2) at 2 to 4 Hz with easing (option 1). Risk: the blank frame per update remains (Verified
   structure, Inferred visibility); DPI-dependent rasterisation remains; centring needs `<p align="center">`.
5. **`file:` frames with hints instead of `data:`.** Pre-render, say, 51 frames (0 to 100 % in 2 % steps) as
   `.svg` files into the extension's temp folder once, and reference
   `file:///…/gauge_050.svg?--x-cmdpal-fit=fit&--x-cmdpal-maxwidth=320&--x-cmdpal-height=160`. Hints apply to
   `file:` (§4), `fit` gives uniform stretch, the `.svg` extension skips sniffing. Risk: writes to the temp
   folder are a new capability (SAFETY-CONTRACT §3 question 2: "Yes", needs an ADR); still one rebuild and
   one blank frame per update; no cache, so each frame is re-read from disk.
6. **`ImageContent` with pre-rendered `.svg` frame files.** Same frames as option 5, but
   `ImageContent.Image = new IconInfo(absolutePath)` per tick swaps only the image (§8) with no markdown
   parse; `MaxWidth`/`MaxHeight` fix the box; the old image stays until the new one loads (Inferred). Risk:
   the same file-write capability as option 5; the zoom overlay button and card chrome cannot be removed;
   the unbounded request rasterises SVG at 256 px wide; `data:` and SVG streams do not work here. Requires
   SDK 0.9 or later (already) and host 0.99 or later.
7. **An Adaptive Card with a raw-SVG `Image` (Microsoft's own pattern).** `FormContent` whose `DataJson`
   carries `data:image/svg+xml;utf8,<svg …>` into an `Image` with explicit px width and height, plus
   `TextBlock`s for the numbers; no actions or inputs on the card, and a sibling markdown block so it is not
   the only content (focus stealing, §9). Risk: on 0.101 it flickers like the Performance Monitor (#46362);
   colours are the named palette; on 0.102 this exact shape becomes flicker-free (PR #50211). A reasonable
   second step if the owner upgrades, not a first step.
8. **A gallery of tiles or a details pane as the dashboard.** Gallery tiles (160×160, 256-px bucket) or the
   details pane (`ContentSize.Large`, markdown body with `data:` images, live since 0.101) with pre-rendered
   frame icons by path (cached after first decode, §6) or live tag pills with colours (§7). Risk: a different
   information architecture for the owner; tiles crop non-square images; icons cannot be `data:`.

**Not viable on 0.101 (Verified unless marked):** SMIL or CSS animation in SVG; `<text>` in SVG; hints on a
`data:` URI; `data:` strings as icons (Inferred); SVG streams as icons; `ProgressPercent` in a status message;
Adaptive Card `ProgressBar`/`Chart.Gauge`; multi-character glyph icons; theme-aware colours in any image;
side-by-side content blocks; waiting for PR #50443.

**Suggested experiment order on the owner's PC:** options 1 + 2 + 3 first (expected smooth and eased, no
new capability, all testable in Core); then option 4 in the same structure at 2 to 4 Hz to see whether the
blank frame is visible; then, only if an image is wanted, option 6 (a temp-folder write is a new capability)
or option 7 after 0.102. Whatever is chosen, plan item 5.1 proceeds as ADR-0013 says: it starts by restoring
`GaugeSvg` from `6034b22` (option 4 reuses it; the text options remove it again) and records the redrawn meter
in the ADR that supersedes ADR-0013. This document ranks; that ADR decides.

## 16. Rules for the building agent, and what only Windows can answer

Rules that follow from the evidence:

- Return the same content instances from `GetContent()`; update by setting properties; never
  `RaiseItemsChanged` from the meter path or from inside `GetContent()`.
- One block for the moving part; static text in its own block.
- Ease in the extension; tick at 100 to 200 ms; skip a tick when the string is unchanged.
- If an SVG is embedded: numeric `width`/`height` and `viewBox` on the root; width ≤ 256; only §4's elements
  and attributes; `stroke-opacity` rather than 8-digit hex; no text; transparent background; centre with
  `<p align="center">`.
- Big numbers in H1/H2 or bold; never H3.
- Do not add a file write, a temp folder, or a network fetch for images without an ADR and a "Yes" in the
  PR's safety questions.
- Keep the measurement loop off the UI path: raise property changes from the timer thread (two cross-process
  hops per set, §5 step 2), not from inside a transfer.

Unknown until a Windows run (all Inferred above):

1. Whether the blank frame of a rebuilt markdown image is visible at 2 to 5 Hz, and whether a constant
   image box turns it into a blink rather than a jump.
2. Whether `IsTextSelectionEnabled` and identical-string `Text` sets trigger a re-render.
3. Glyph width uniformity of block characters in the inline-code and Consolas fonts.
4. Whether `ImageContent` keeps the previous image visible until the next one is decoded.
5. Whether animated GIF plays in markdown and inside a card, and whether `ImageIcon` plays it at all.
6. The real cost of a full card or markdown rebuild on the owner's laptop (no benchmark exists).
7. Whether the DPI mismatch of §4 rule 3 is what the owner saw; a screenshot at 100 % and 150 % would
   tell.

## 17. Sources

Microsoft Learn: "Display markdown content in Command Palette extensions", "Get user input with forms",
"Update a list of commands", "Command results", "Adding commands", "Command Palette Extension Samples",
"Command Palette Settings", the `IDetails`, `IGridProperties`, `IGalleryGridLayout`, `IIconInfo`, `ITag`,
`IStatusMessage`, `FormContent`, `MarkdownContent` reference pages; WinUI `BitmapImage` and `SvgImageSource`
reference; Direct2D "SVG Support"; Adaptive Cards "Template Language" and "WinUI 3 SDK getting started".

GitHub, microsoft/PowerToys: release notes v0.90.0 to v0.101.2684.0; issues #38959, #44915, #46362, #46419,
#47745, #49361, #49435, #50430, #50483; pull requests #41754, #43964, #44545, #48070, #50151, #50187,
#50188, #50189, #50190, #50191, #50211, #50443, #50485. CommunityToolkit/Labs-Windows
`components/MarkdownTextBlock`. microsoft/AdaptiveCards `source/uwp`, `source/shared`.
microsoft/botbuilder-dotnet `ExpressionType.cs`. raycast/extensions `extensions/speedtest`.
mrgenty/NetSpeed-CmdPal, zhang-astronaut/HardwareMonitorCmdPal, Tai-Yng/Pomodoro-CmdPal.
