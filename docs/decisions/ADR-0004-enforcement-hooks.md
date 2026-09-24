# ADR-0004: Enforce project rules with git hooks and Claude Code hooks

Status: accepted · Date: 2026-09-24

## Context
The owner requires deterministic enforcement on all agents, including subagents and non-Claude agents, and full
traceability. Instructions in markdown are advisory; hooks are not.

## Decision
Two layers, one shared rule script:
- `scripts/check.sh` holds the rules (ids R1–R8, R10, R12; R9 and R11 are commit-message trailers, R13 runs in CI). `.githooks/pre-commit` runs it on staged files; CI runs it on all.
  `.githooks/commit-msg` (C1, R9) and `.githooks/pre-push` (P1, P2) cover history and branch protection. Any git
  client, human or agent, runs them once `scripts/setup.sh` sets `core.hooksPath`.
- `.claude/settings.json` adds Claude Code hooks (G1–G8, W1–W2, S1) that block bypasses and protect files before
  the tool call happens. They apply to subagents automatically.
Changing any guard file requires a `Guard-Change:` commit trailer so every weakening is visible in `git log`.

## Consequences
Agents occasionally get blocked and must explain instead of working around; that is the point. The git layer needs
`bash` (Git for Windows provides it). Determined humans can still bypass; review remains necessary.
