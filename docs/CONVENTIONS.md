# Conventions

Short and enforced where possible. Rule ids in brackets refer to `docs/SECURITY.md`.

## Naming

| Thing | Convention | Example |
|-------|-----------|---------|
| C# namespaces | `SpeedTest.<Area>` | `SpeedTest.Core`, `SpeedTest.Extension` |
| Types, methods, properties | PascalCase, one type per file, file name = type name | `SpeedMeasurer.cs` |
| Private fields | `_camelCase` | `_httpClient` |
| Locals, parameters | `camelCase` | `bytesReceived` |
| Constants | PascalCase | `DefaultDurationSeconds` |
| Async methods | `Async` suffix, take a `CancellationToken` | `MeasureAsync(ct)` |
| Setting ids (persisted strings) | camelCase, defined once in `SettingsManager` | `defaultView` |
| Test methods | `Method_Scenario_Expectation` | `Format_BelowOneMbps_UsesKbps` |
| Docs and ADRs | `docs/UPPERCASE.md`, `docs/decisions/ADR-NNNN-kebab-title.md` | `ADR-0002-cloudflare-backend.md` |
| Owner-facing HTML explainers | `docs/UPPERCASE.html`, the stem of the technical document it explains; one self-contained file that loads nothing from the network (no scripts, styles, fonts, or images from a URL) | `CMDPAL-RENDERING.html` |
| Branches | `<type>/<kebab-topic>` that says what the branch delivers. Auto-generated names (`claude/<word>-<word>-<id>`) are refused by the pre-push hook (P3); rename with `git branch -m` first | `feat/latency-phase` |

The project and assembly keep the template name `internet_speed_test_extension` (ADR-0005). The MSIX identity is
`InternetSpeedTestExtension`, because package names may not contain underscores (ADR-0009).
Types inside it use the `SpeedTest.Extension` namespace and PascalCase names.

## C# style

- `Nullable` enabled, warnings are errors in CI. `sealed` and `internal` by default.
- Records for immutable data (`SpeedTestResult`). No inheritance for code reuse.
- `HttpClient` is injected, never created inside logic classes, so tests can fake the network.
- Every network call has a timeout and a byte bound. Every loop that waits on the network checks the token.
- No `Console.WriteLine` outside `Program.cs`. Diagnostics via `System.Diagnostics.Debug` only, never with IP addresses.
- `GetItems()` and `GetContent()` are hot paths: return cached state, never start work there.
- Never call `RaiseItemsChanged()` from inside `GetItems()`/`GetContent()` or from code they call: the host answers it
  by calling them again. A content page updates live by changing its content objects (`MarkdownContent.Body`).

## Git

- Conventional Commits (`C1`): `type(scope): summary`. Types: `feat fix docs chore refactor test ci build perf revert`.
  Scopes: `core`, `ext`, `tests`, `ci`, `docs`, `hooks`.
- One logical change per commit. Code or test change (`src/`, `tests/`, extension) ⇒ `CHANGELOG.md` line under `Unreleased` in the same commit (`R8`).
- Guard file change ⇒ `Guard-Change: <reason>` trailer (`R9`).
- Never commit on `main` (`R1`), never push to `main` (`P1`, `G3`), never rewrite pushed history (`P2`, `G2`, `G6`, `G8`).
- Merge to `main` only through a pull request.
- Who did what (ADR-0012): a human commits as themselves. A building agent commits with author name `Claude Code`
  (set by the `env` block in `.claude/settings.json`) and the owner as committer, plus a `Co-Authored-By` trailer.
  A reviewing agent never commits; it posts as `chatgpt-codex-connector` (Codex) or as `github-actions[bot]`
  under the heading "Independent review (Claude)". `git log --format='%h %an | %cn %s'` tells the first two
  apart. Every identity uses the owner's GitHub noreply address; the real address never goes into a commit.

## Pull requests

- One PR per branch. A session that changes rule or guard files and also files outside R14's exceptions (session
  log, plan, changelog, ADRs) uses two branches and two PRs (R14, `AGENTS.md` §2). Title is a Conventional Commit line. Body follows `.github/pull_request_template.md`.
- The PR body states what was verified and how (CI green, unit tests, manual test on Windows) and what was not.
- CI must be green. Reviewers (human or agent) check the PR against `docs/SECURITY.md` and this file.

## Documentation

- `CHANGELOG.md`: Keep a Changelog format. Users read it.
- `docs/SESSION-LOG.md`: one entry per agent session, newest first. Agents read it.
- `docs/decisions/`: ADRs, numbered, never edited after acceptance (add a superseding ADR instead).
- `docs/PLAN.md`: roadmap with status. Updated at the end of every session.
- `docs/UPPERCASE.html`: an explainer the owner reads in a browser, paired with the `.md` of the same stem that
  agents read. The `.md` is the source of truth; the two must not disagree. Reviewed under "Documentation-only
  pull requests" in `AGENTS.md`.
