#!/usr/bin/env bash
# Claude Code Stop hook: if this session changed the repo, the hand-off note in docs/SESSION-LOG.md
# must have been updated too. Blocks (exit 2) until it is. Read-only sessions are not affected.
# Without a session marker (SessionStart did not run) only the working tree is inspected; history is
# never scanned from time zero, because that would let an old SESSION-LOG commit satisfy the check.
set -u
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || exit 0)"
cd "$ROOT"
MARK_FILE="$(git rev-parse --git-path claude-session-start 2>/dev/null)"
MARK=""
[ -f "$MARK_FILE" ] && MARK="$(cat "$MARK_FILE")"
CHANGED=0
[ -n "$(git status --porcelain)" ] && CHANGED=1
[ -n "$MARK" ] && [ -n "$(git log --since="@$MARK" --oneline 2>/dev/null)" ] && CHANGED=1
[ "$CHANGED" = 0 ] && exit 0
LOG_TOUCHED=0
git status --porcelain docs/SESSION-LOG.md | grep -q . && LOG_TOUCHED=1
[ -n "$MARK" ] && [ -n "$(git log --since="@$MARK" --oneline -- docs/SESSION-LOG.md 2>/dev/null)" ] && LOG_TOUCHED=1
[ "$LOG_TOUCHED" = 1 ] && exit 0
echo "BLOCKED by scripts/hooks/stop-check.sh (S1): the repo changed this session but docs/SESSION-LOG.md was not updated. Add a hand-off entry (what was done, what is verified, what is next), commit, then stop." >&2
exit 2
