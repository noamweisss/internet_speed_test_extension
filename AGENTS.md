# AGENTS.md

Rules for every agent (and human) working in this repository. Vendor-neutral: `CLAUDE.md` and
`.github/copilot-instructions.md` only point here. If a tool-specific file disagrees with this one, this one wins.

## 1. Read order (five minutes)

1. This file.
2. `docs/SESSION-LOG.md` – the latest hand-off note says exactly where things stand.
3. `docs/PLAN.md` – the roadmap and what the current session is expected to deliver.
4. `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, `docs/SECURITY.md`, `docs/TESTING.md` – as needed.
5. `.github/instructions/cmdpal-extension.instructions.md` – Command Palette SDK reference from the template.
6. `docs/CMDPAL-RENDERING.md` – what the Command Palette host renders and how (research, session 5); read it
   before any work on the meter view.

## 2. Non-negotiables (machine-enforced)

These are enforced by `.githooks/*` (any git user) and `.claude/settings.json` (Claude Code and its subagents).
Every rule has an id (`R1`, `G2`, `W1`, `C1`, `P1`, `S1`) printed when it fires, defined in `docs/SECURITY.md`.

- Work on a feature branch. Never commit on `main`; never push to `main`; never force-push or rewrite history.
- Never bypass hooks (the `no-verify` flag), never change `core.hooksPath` except through `scripts/setup.sh`.
- Commit messages follow Conventional Commits. Code or test changes ship with a `CHANGELOG.md` line in the same commit.
- Changes to guard files (`.githooks/`, `.claude/`, `scripts/`, `.github/workflows/`, `AGENTS.md`, `CLAUDE.md`)
  need a `Guard-Change: <reason>` trailer in the commit message.
- `internet_speed_test_extension/Program.cs` (the COM host) is not edited by agents (W1). If a human decides it must
  change, the commit carries a `Protected-Change: <why>` trailer (R11) and references an ADR.
- The extension talks only to hosts listed in `scripts/allowed-hosts.txt`. Adding a host requires a human decision,
  an ADR, and a `docs/SECURITY.md` update.
- No process spawning, native interop, `unsafe`, reflection loading, plain `http://`, or TLS validation overrides.
- Before a session ends, `docs/SESSION-LOG.md` gets a hand-off entry if anything changed.
- A PR that touches a safety-sensitive file (`docs/SAFETY-CONTRACT.md` §2) must explain, in plain language, what
  the extension can now do that it could not before, under "Safety impact" in the PR body (R13).
- Merging a pull request is the owner's decision. An agent merges only when the owner asks for that specific PR
  in the current session and says why; the request is quoted in `docs/SESSION-LOG.md`. Reviews are comments,
  never verdicts. A review thread is resolved by the building agent only after the reviewer that opened it has
  reviewed the fixed commit and not repeated the finding; the resolving reply names that review. The owner may
  resolve or dismiss anything.

## 3. Non-negotiables (judgement, reviewed by humans and audit agents)

- `docs/SAFETY-CONTRACT.md` is the owner's promise list. Never make it false. Reviewing agents answer its five
  questions on every PR (the exact prompt is `docs/REVIEW-PROMPT.md`); building agents answer them in the PR
  body when any is "Yes".
- Less code is better. Prefer deleting over adding. No speculative abstractions, no "might need later".
- Meaningful names for everything: branches, files, types, variables, commits, PRs. No generated or placeholder
  names. If a name needs a comment to explain it, pick a better name.
- Every decision that a reviewer could question gets an ADR in `docs/decisions/` (see `ADR-0001`).
- Everything user-visible or reviewer-relevant is documented in `docs/`. Code comments explain *why*, not *what*.
- Testable logic lives in `src/SpeedTest.Core` (no Windows or Command Palette dependency) and has unit tests.
  The extension project is a thin adapter over Core.
- Do not add NuGet packages without an ADR. Do not add tooling that only works on one OS unless CI covers it.
- Never log or persist the user's IP address or any measurement result beyond the current session.
- When blocked, write down what is blocked and why in `docs/SESSION-LOG.md` instead of working around a guard.

## 4. Session workflow

1. Start: the SessionStart hook installs git hooks and prints the latest hand-off note. Read it.
   If the harness put you on an auto-generated branch (`claude/<word>-<word>-<id>`), rename it before anything else:
   `git branch -m <type>/<meaningful-topic>` (see `docs/CONVENTIONS.md`). The pre-push hook (P3) refuses the
   generated names. The owner has explicitly authorised this rename; pushing to the renamed branch is the
   designated branch for the session. Delete the generated remote branch if it was already pushed.
2. Pick the next items from `docs/PLAN.md` for the current session. Do not skip ahead unless the plan says so.
3. Small commits, each passing `scripts/check.sh` (runs automatically on commit).
4. Run tests (`docs/TESTING.md`). Push to the feature branch; CI builds the Windows extension.
5. End: update `docs/SESSION-LOG.md` (done / verified / not verified / next), `docs/PLAN.md` status, `CHANGELOG.md`.
   Open or update the pull request using `.github/pull_request_template.md`.

## 5. Map

| Path | What |
|------|------|
| `internet_speed_test_extension/` | Command Palette extension (Windows-only, MSIX). Thin UI layer. |
| `src/SpeedTest.Core/` | Measurement engine, result model, formatting. Cross-platform, no UI. |
| `tests/SpeedTest.Core.Tests/` | xUnit tests for Core. Run anywhere. |
| `scripts/` | `check.sh` (rules), `setup.sh` (hooks install), `hooks/` (Claude Code hooks), `allowed-hosts.txt`. |
| `.githooks/` | pre-commit, commit-msg, pre-push. Installed by `scripts/setup.sh`. |
| `install/` | `Install-SpeedTestExtension.ps1`: the owner's install script (Windows, ADR-0008). Safety-sensitive (R13). |
| `.github/workflows/` | CI: Windows build, package, and install test of the extension; Core tests; `check.sh all`. |
| `docs/` | Plan, architecture, conventions, security, testing, session log, ADRs. |

## Code Review Rules

For every reviewing agent (Codex reads this section; the Claude workflow is told to read it) and every human
reviewer. The full prompt is `docs/REVIEW-PROMPT.md`; its severities map P0 = Blocker, P1 = Major, P2 = Minor,
P3 = Nit. The owner wants two things guarded with equal weight: the safety promises, and a codebase that stays
small, tested, and cheap to change.

### Safety questions

Answer the five questions of `docs/SAFETY-CONTRACT.md` §3 on every pull request, each with Yes or No and a
file:line for every Yes. A Yes without a linked ADR in `docs/decisions/` is P0: the extension reaching a host
other than speed.cloudflare.com; reading, writing, or deleting files, registry keys, or processes; storing,
logging, or sending anything about the user (IP, ISP, location, results); a new dependency, Windows capability,
or weakened guard; network input used without bounds on size, time, or format.

### Guards

A change under `.githooks/`, `.claude/`, `scripts/`, `.github/workflows/`, `AGENTS.md`, or `CLAUDE.md` that
makes a rule weaker, adds a bypass, or removes a check is P1 unless the pull request's "Safety impact" section
justifies it and names an ADR. A workflow that holds a secret and executes anything a pull request controls
is P0.

### Correctness

A finding that crashes, hangs, or mis-measures on a real input is P1: say what goes wrong, under which input,
and the smallest fix, and verify it against the code before posting. Treat a network read without a byte
bound or a timeout as P1. Style is never above P3.

### Design and maintainability

The project is deliberately small (`docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`). Each of these is P1, with
the file:line and the smaller alternative:

- Logic in the wrong layer: measurement, parsing, formatting, or maths added under `internet_speed_test_extension/`
  instead of `src/SpeedTest.Core` (ADR-0003). The extension is a thin adapter over Core.
- Testable logic without a unit test in `tests/SpeedTest.Core.Tests`, or a bug fix without the test that would
  have caught it (`docs/TESTING.md`).
- A second copy of a calculation, a parser, or a constant instead of a call to the existing one. A constant
  that belongs in `SpeedTestOptions` or `CloudflareEndpoints` hard-coded elsewhere.
- Speculative structure: an interface, base class, option, setting, or parameter with one implementation or
  no caller. Less code is better; delete over add.
- Hot paths: work started inside `GetItems()` or `GetContent()`, `RaiseItemsChanged()` called from them or
  from code they call, or a network call, file read, or large allocation per render.
- Waste on the measurement path: an `HttpClient` created per call, a task never awaited, a stream or response
  never disposed, a loop that spins or polls without delay, per-sample allocations inside the sampling loop.
- State outside `SpeedTestSession`: new statics, singletons, or shared mutable fields; Core stays stateless per
  run.
- A name that needs a comment to be understood, a placeholder or generated name, a file holding more than one
  type (`docs/CONVENTIONS.md`).

Formatting, comment wording, and ordering are P3 at most.

### Documentation and instructions

Report these in a separate list from code findings, never mixed with them. Each is P1 when it could send an
agent or a human the wrong way, P2 otherwise:

- A document that contradicts the code, another document, or an ADR (a status, a file name, a rule, a number).
- An instruction in `AGENTS.md`, `docs/`, a hook message, or a PR template that can be read two ways, names a
  file or rule that does not exist, or asks for something the guards prevent.
- A change in behaviour, guard, or interface without the matching change in `docs/`, `CHANGELOG.md`, or an ADR.
- A hand-off note (`docs/SESSION-LOG.md`) or plan status that does not match what the pull request did.

Wording and tone are P3.

### Documentation-only pull requests

ADR-0015 records this decision. A pull request is documentation-only when every file in its diff is a document
that states no rule: a `.md` file at the repository root or under `docs/`, or a `.html` file under `docs/`, and
not one of the rule files, which are exactly `AGENTS.md`, `CLAUDE.md`, everything under `.github/`, `docs/SAFETY-CONTRACT.md`,
`docs/SECURITY.md`, `docs/REVIEW-PROMPT.md`, `docs/CONVENTIONS.md` and `docs/TESTING.md`. Status and record
files are documents (`docs/PLAN.md`, `docs/SESSION-LOG.md`, `CHANGELOG.md`, `README.md`, `docs/INSTALL.md`,
`docs/ARCHITECTURE.md`, the ADRs, research documents and their explainers), so the hand-off and plan updates
this subsection requires keep a pull request documentation-only. A diff that also touches anything else (a
rule file, a guard file, a workflow, a script, a project file, code, a test, an asset) is reviewed under every
rule above, with the two rules for research documents below applied to its documents. Review a
documentation-only pull request like this:

- Safety questions: the five answers come from the diff as always. A document cannot reach a host, touch a
  file, or add a dependency, so with only documents in the diff every answer is "No".
- Correctness and design: the code rules above have nothing to check. Do not ask for a unit test, a
  Core-layer move, or an ADR for text: a research document records what was found and what could be tried,
  and the pull request that implements a choice carries the ADR.
- Documentation and instructions: the rules above apply in full, plus two for research documents. A statement
  about an external system (Command Palette, PowerToys, the SDK, a renderer) needs a source and a confidence
  label (`Verified`, `Inferred`, `Unknown`); one without either is P2, and the reviewer does not reproduce the
  external fact. An HTML explainer under `docs/` is reviewed for what it says, against the document it explains:
  a claim in one that the other contradicts is P1. Its markup and styles get no findings; its script is
  reviewed for what it does, since it runs when the owner opens the page: a script that does anything beyond
  driving the page's own content, or a page that loads anything from the network, is P1 (`docs/CONVENTIONS.md`).
- `CHANGELOG.md` is for users of the extension; a document for agents or the owner needs no line (R8 covers
  code only). The hand-off entry and the plan status are still required.

### Completeness

Report in one review everything you can verify, across every group above; never keep a finding for the next
round. A pull request should need one fix round, not a dozen. If a fix would open a new hole, say so in the
same finding.

### Conduct

Review the code, not other reviewers' threads or the author's replies. Never change code, never approve or
request changes, never speak for another reviewer. On a re-review, do not repeat a finding the current head
has fixed; a finding not repeated counts as fixed.
