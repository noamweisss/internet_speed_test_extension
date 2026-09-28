# AGENTS.md

Rules for every agent (and human) working in this repository. Vendor-neutral: `CLAUDE.md` and
`.github/copilot-instructions.md` only point here. If a tool-specific file disagrees with this one, this one wins.

## 1. Read order (five minutes)

1. This file.
2. `docs/SESSION-LOG.md` – the latest hand-off note says exactly where things stand.
3. `docs/PLAN.md` – the roadmap and what the current session is expected to deliver.
4. `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, `docs/SECURITY.md`, `docs/TESTING.md` – as needed.
5. `.github/instructions/cmdpal-extension.instructions.md` – Command Palette SDK reference from the template.

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

For every reviewing agent (Codex reads this section; the Claude workflow carries the same rules in its prompt)
and every human reviewer. The full prompt is `docs/REVIEW-PROMPT.md`.

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

### Conduct

Review the code, not other reviewers' threads or the author's replies. Never change code, never approve or
request changes, never speak for another reviewer. On a re-review, do not repeat a finding the current head
has fixed; a finding not repeated counts as fixed.
