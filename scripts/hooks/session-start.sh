#!/usr/bin/env bash
# Claude Code SessionStart hook: installs git hooks, records the session start marker and a snapshot of
# uncommitted changes (so stop-check.sh ignores edits made before the session), and prints
# the agent rules pointer plus the latest hand-off note so every session starts with the same context.
# The marker and snapshot live in git's per-worktree directory (git rev-parse --git-path), so linked worktrees
# work too. They are named after the session id from the hook input, so two sessions in one worktree do not
# overwrite each other's baseline, and a resume or compaction (same id) keeps the original baseline.
set -u
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT"
bash scripts/setup.sh >/dev/null 2>&1 || true
SESSION=""
# The hook input is JSON on stdin; the session id is read without jq so the hook also works where jq is missing.
[ -t 0 ] || SESSION="$(head -c 65536 | sed -nE 's/.*"session_id"[[:space:]]*:[[:space:]]*"([^"]*)".*/\1/p' | head -n1 | tr -cd 'A-Za-z0-9-')"
MARK_FILE="$(git rev-parse --git-path "claude-session${SESSION:+-$SESSION}-start")"
TREE_FILE="$(git rev-parse --git-path "claude-session${SESSION:+-$SESSION}-tree")"
if [ -z "$SESSION" ] || [ ! -f "$MARK_FILE" ]; then
  date +%s > "$MARK_FILE" || { echo "session-start: could not write the session marker at $MARK_FILE" >&2; exit 1; }
  bash scripts/hooks/tree-state.sh > "$TREE_FILE" ||
    { echo "session-start: could not write the working tree snapshot at $TREE_FILE" >&2; exit 1; }
fi
# Markers and snapshots of sessions that ended more than 30 days ago are not needed any more.
find "$(git rev-parse --git-path .)" -maxdepth 1 -type f -name 'claude-session-*' -mtime +30 -delete 2>/dev/null
echo "Read AGENTS.md before doing anything. Branch: $(git symbolic-ref --short HEAD 2>/dev/null)."
echo "Latest hand-off note (docs/SESSION-LOG.md):"
awk '/^## /{n++} n==1' docs/SESSION-LOG.md 2>/dev/null | head -40
exit 0
