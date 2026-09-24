#!/usr/bin/env bash
# Claude Code SessionStart hook: installs git hooks, records the session start marker, and prints
# the agent rules pointer plus the latest hand-off note so every session starts with the same context.
# The marker lives in git's per-worktree directory (git rev-parse --git-path), so linked worktrees work too.
set -u
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT"
bash scripts/setup.sh >/dev/null 2>&1 || true
MARK_FILE="$(git rev-parse --git-path claude-session-start)"
date +%s > "$MARK_FILE" || { echo "session-start: could not write the session marker at $MARK_FILE" >&2; exit 1; }
echo "Read AGENTS.md before doing anything. Branch: $(git symbolic-ref --short HEAD 2>/dev/null)."
echo "Latest hand-off note (docs/SESSION-LOG.md):"
awk '/^## /{n++} n==1' docs/SESSION-LOG.md 2>/dev/null | head -40
exit 0
