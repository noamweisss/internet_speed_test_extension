#!/usr/bin/env bash
# Claude Code SessionStart hook: installs git hooks, records the session start marker, and prints
# the agent rules pointer plus the latest hand-off note so every session starts with the same context.
set -u
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT"
bash scripts/setup.sh >/dev/null 2>&1 || true
date +%s > .git/claude-session-start
echo "Read AGENTS.md before doing anything. Branch: $(git symbolic-ref --short HEAD 2>/dev/null)."
echo "Latest hand-off note (docs/SESSION-LOG.md):"
awk '/^## /{n++} n==1' docs/SESSION-LOG.md 2>/dev/null | head -40
exit 0
