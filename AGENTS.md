# AGENTS.md

Rules for every agent and human working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md`
only point here. If a tool-specific file disagrees with this one, this one wins.

## 1. Read order (five minutes)

1. This file.
2. `docs/SESSION-LOG.md`: the latest hand-off note says where things stand.
3. `docs/PLAN.md`: the roadmap and what the current session delivers.
4. `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, `docs/SECURITY.md`, `docs/TESTING.md`, as needed.
5. `.github/instructions/cmdpal-extension.instructions.md`: Command Palette SDK reference from the template.
6. `docs/CMDPAL-RENDERING.md`: what the Command Palette host renders and how. Read it before any work on the
   meter view.

## 2. Non-negotiables (machine-enforced)

Enforced by `.githooks/*` (any git user) and `.claude/settings.json` (Claude Code and its subagents). Every rule
has an id (`R1`, `G2`, `W1`, `C1`, `P1`, `S1`) printed when it fires and defined in `docs/SECURITY.md`.

- Work on a feature branch. Never commit on `main`, never push to `main`, never force-push or rewrite history.
- Never bypass hooks (the `no-verify` flag). Never change `core.hooksPath` except through `scripts/setup.sh`.
- Commit messages follow Conventional Commits. A code or test change ships with a `CHANGELOG.md` line in the
  same commit.
- A change to a guard file (`.githooks/`, `.claude/`, `scripts/`, `.github/workflows/`, `AGENTS.md`, `CLAUDE.md`)
  needs a `Guard-Change: <reason>` trailer in the commit message.
- Agents do not edit `internet_speed_test_extension/Program.cs`, the COM host (W1). If a human decides it must
  change, the commit carries a `Protected-Change: <why>` trailer (R11) and names an ADR.
- The extension talks only to hosts listed in `scripts/allowed-hosts.txt`. A new host needs a human decision,
  an ADR, and a `docs/SECURITY.md` update.
- No process spawning, native interop, `unsafe`, reflection loading, plain `http://`, or TLS validation overrides.
- Before a session ends, `docs/SESSION-LOG.md` gets a hand-off entry if anything changed.
- A PR that touches a safety-sensitive file (`docs/SAFETY-CONTRACT.md` §2) explains, in plain language, what
  the extension can now do that it could not before, under "Safety impact" in the PR body (R13).
- A PR that changes a rule or guard file (`AGENTS.md`, `CLAUDE.md`, `.coderabbit.yaml`, `.github/`, `.githooks/`,
  `.claude/`, `scripts/`, `docs/SAFETY-CONTRACT.md`, `docs/SECURITY.md`, `docs/REVIEW-PROMPT.md`,
  `docs/CONVENTIONS.md`, `docs/TESTING.md`) changes nothing else except `docs/SESSION-LOG.md`, `docs/PLAN.md`,
  `CHANGELOG.md` and ADRs (R14, CI). The rest goes in a second PR; merge the rule PR first when the other
  depends on it (ADR-0016).
- Merging is the owner's decision. An agent merges only when the owner asks for that PR in the current session
  and says why; the request is quoted in `docs/SESSION-LOG.md`. A reviewer's verdict gates nothing: the `main`
  ruleset requires resolved threads, not approvals. The reviewer resolves a thread after the building agent
  pushed the fix and named the commit in that thread. A thread it leaves open is the owner's: resolve it,
  dismiss it, or hand it back to the building agent for one more round.

## 3. Non-negotiables (judgement, reviewed by humans and audit agents)

- `docs/SAFETY-CONTRACT.md` is the owner's promise list. Never make it false. The reviewer answers its five
  questions on every PR (they are in `.coderabbit.yaml`); a building agent answers them in the PR body when
  any is "Yes".
- Less code is better. Prefer deleting over adding. No speculative abstractions, no "might need later".
- Meaningful names for everything: branches, files, types, variables, commits, PRs. No generated or placeholder
  names. If a name needs a comment to explain it, pick a better name.
- Every decision a reviewer could question gets an ADR in `docs/decisions/` (see `ADR-0001`).
- Everything user-visible or reviewer-relevant is documented in `docs/`. Code comments explain why, not what.
- Testable logic lives in `src/SpeedTest.Core` (no Windows or Command Palette dependency) and has unit tests.
  The extension project is a thin adapter over Core.
- No NuGet package without an ADR. No tooling that works on one OS only, unless CI covers it.
- Never log or persist the user's IP address or any measurement result beyond the current session.
- When blocked, write what is blocked and why in `docs/SESSION-LOG.md`. Do not work around a guard.

## 4. Session workflow

1. Start: the SessionStart hook installs the git hooks and prints the latest hand-off note. Read it. On an
   auto-generated branch (`claude/<word>-<word>-<id>`), first run `git branch -m <type>/<meaningful-topic>`
   (`docs/CONVENTIONS.md`): the pre-push hook (P3) refuses generated names, and the owner has authorised the
   rename. Delete the generated remote branch if it was already pushed.
2. Pick the next items from `docs/PLAN.md`. Do not skip ahead unless the plan says so.
3. Small commits, each passing `scripts/check.sh` (runs on commit).
4. Run the tests (`docs/TESTING.md`). Push to the feature branch; CI builds the Windows extension.
5. End: update `docs/SESSION-LOG.md` (done, verified, not verified, next), `docs/PLAN.md` status,
   `CHANGELOG.md`. Open or update the pull request from `.github/pull_request_template.md`.
6. Review (ADR-0016, ADR-0018). CodeRabbit reviews only when asked. When the branch is finished and pushed,
   comment `@coderabbitai review` on the PR, once. Findings arrive in 5 to 15 minutes as review threads; none
   means the PR is ready. Fix everything in one push; for a documentation finding, fix every occurrence of the
   same concept in the diff. After the push, reply in each thread: "Fixed in <sha>, please verify", or why it
   is not changed. The reviewer checks the head and resolves what it accepts. Reply after the push, never
   before: it believes the reply, and "not yet pushed" leaves the thread open for good. When the PR is
   otherwise ready to merge (CI green, every thread answered, docs in step), request one re-review with
   `@coderabbitai review`. The repository gets one review per hour, shared by every open PR; a trigger inside
   that hour is refused, and `@coderabbitai rate limit` shows the slot for free. Readiness is the reason to
   trigger, not the hour. That ends the loop, unless the round reports a Blocker or Major in code or a safety
   finding (a "Yes" to a question of `docs/SAFETY-CONTRACT.md` §3, or a weakened guard); only then fix and
   request another round. A documentation finding after the fix round: fix it if cheap, answer once in its
   thread, no review request. When the owner asks the reviewer to "agree", that means no open Blocker or
   Major, not a round with zero comments. Then report the PR as ready.

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

For CodeRabbit (ADR-0018) and every human reviewer. Severities: P0 = Blocker, P1 = Major, P2 = Minor, P3 = Nit.
Two things are guarded with equal weight: the safety promises, and a codebase that stays small, tested, and
cheap to change.

### Safety questions

Answer the five questions of `docs/SAFETY-CONTRACT.md` §3 on every pull request, Yes or No, with a file:line
for every Yes. A Yes without a linked ADR in `docs/decisions/` is P0.

### Guards

A change under `.githooks/`, `.claude/`, `scripts/`, `.github/workflows/`, `AGENTS.md`, or `CLAUDE.md` that
makes a rule weaker, adds a bypass, or removes a check is P1 unless the pull request's "Safety impact" section
justifies it and names an ADR. A workflow that holds a secret and executes anything a pull request controls
is P0.

A pull request that changes a rule or guard file changes nothing else except `docs/SESSION-LOG.md`,
`docs/PLAN.md`, `CHANGELOG.md` and ADRs (R14, §2, checked in CI). When a rule change needs a matching change
in another document, name it as a follow-up for its own pull request, not as a finding here.

### Correctness

A finding that crashes, hangs, or mis-measures on a real input is P1: say what goes wrong, under which input,
and the smallest fix, and verify it against the code before posting. A network read without a byte bound or a
timeout is P1. Style is never above P3.

### Design and maintainability

The project is deliberately small (`docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`). Each of these is P1, with
the file:line and the smaller alternative:

- Logic in the wrong layer: measurement, parsing, formatting, or maths under `internet_speed_test_extension/`
  instead of `src/SpeedTest.Core` (ADR-0003). The extension is a thin adapter over Core.
- Testable logic without a unit test in `tests/SpeedTest.Core.Tests`, or a bug fix without the test that would
  have caught it (`docs/TESTING.md`).
- A second copy of a calculation, a parser, or a constant instead of a call to the existing one. A constant
  that belongs in `SpeedTestOptions` or `CloudflareEndpoints` hard-coded elsewhere.
- Speculative structure: an interface, base class, option, setting, or parameter with one implementation or
  no caller.
- Hot paths: work started inside `GetItems()` or `GetContent()`, `RaiseItemsChanged()` called from them or
  from code they call, or a network call, file read, or large allocation per render.
- Waste on the measurement path: an `HttpClient` created per call, a task never awaited, a stream or response
  never disposed, a loop that spins or polls without delay, per-sample allocations inside the sampling loop.
- State outside `SpeedTestSession`: new statics, singletons, or shared mutable fields. Core stays stateless per
  run.
- A name that needs a comment to be understood, a placeholder or generated name, a file holding more than one
  type (`docs/CONVENTIONS.md`).

Formatting, comment wording, and ordering are P3 at most.

### Documentation and instructions

Report these in a separate list from code findings. Each is P1 when it could send an agent or a human the
wrong way, P2 otherwise:

- A document that contradicts the code, another document, or an ADR (a status, a file name, a rule, a number).
- An instruction in `AGENTS.md`, `docs/`, a hook message, or a PR template that can be read two ways, names a
  file or rule that does not exist, or asks for something the guards prevent.
- A change in behaviour, guard, or interface without the matching change in `docs/`, `CHANGELOG.md`, or an ADR.
- A hand-off note (`docs/SESSION-LOG.md`) or plan status that does not match what the pull request did.

Wording and tone are P3.

### Documentation-only pull requests

ADR-0015. A pull request is documentation-only when its diff holds only documents that state no rule: `.md`
files at the repository root or under `docs/`, and `.html` files under `docs/`, none of them a rule file (the
list in §2). The plan, the session log, the changelog, `README.md`, `docs/INSTALL.md`, `docs/ARCHITECTURE.md`,
the ADRs, research documents and their explainers are documents. Anything else in the diff (a rule or guard
file, a workflow, a script, a project file, code, a test, an asset) makes it an ordinary pull request, reviewed
under every rule above, with the two research rules below applied to its documents. For a documentation-only
pull request:

- Safety questions: every answer is "No". A document cannot reach a host, touch a file, or add a dependency.
- Correctness and design: nothing to check. Do not ask for a unit test, a Core-layer move, or an ADR for text;
  a research document records what was found, and the pull request that implements a choice carries the ADR.
- Documentation and instructions: the rules above, plus two for research documents. A statement about an
  external system (Command Palette, PowerToys, the SDK, a renderer) needs a source and a confidence label
  (`Verified`, `Inferred`, `Unknown`); without them it is P2, and the reviewer does not reproduce the fact. An
  HTML explainer under `docs/` is reviewed against the document it explains: a contradiction between them is
  P1; its markup and styles get no findings; a script that does more than drive the page's own content, or a
  page that loads anything from the network, is P1 (`docs/CONVENTIONS.md`).
- `CHANGELOG.md` is for users of the extension; a document for agents or the owner needs no line (R8 covers
  code only). The hand-off entry and the plan status are still required.

### Completeness

Report in one review everything you can verify, across every group above; never keep a finding for the next
round. If a fix would open a new hole, say so in the same finding. A pull request gets one fix round and one
re-review; only a Blocker or Major in code, or a safety finding, opens another (§4 step 6). A documentation
finding raised after the fix round is fixed if cheap and answered once, not re-reviewed, so report it in the
first round.

### Conduct

Form findings from the code, not from the author's replies. In a thread, verify the commit the reply names
against the current head and say fixed or not fixed. Never change code. A verdict (approve, request changes)
gates nothing; the merge is the owner's decision. On a re-review, do not repeat a finding the current head has
fixed; a finding not repeated counts as fixed.
