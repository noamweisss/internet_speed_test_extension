#!/usr/bin/env bash
# Claude Code PreToolUse hook for Edit/Write/MultiEdit. Blocks (exit 2) writes to protected files.
# Protected files are ones the template says never to change, or whose change is a security decision.
set -u
FILE="$(jq -r '.tool_input.file_path // empty')"
[ -z "$FILE" ] && exit 0
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
REL="${FILE#$ROOT/}"
case "$REL" in
  internet_speed_test_extension/Program.cs)
    echo "BLOCKED by scripts/hooks/guard-write.sh (W1): Program.cs is the COM host from the template and must not change (AGENTS.md)." >&2; exit 2;;
  scripts/allowed-hosts.txt)
    echo "BLOCKED by scripts/hooks/guard-write.sh (W2): adding a network host is a security decision. Ask the human, then record an ADR in docs/decisions and update docs/SECURITY.md before editing." >&2; exit 2;;
esac
exit 0
